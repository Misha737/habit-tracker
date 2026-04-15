using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkflowService.Domain;
using WorkflowService.Infrastructure;

namespace WorkflowService.Application;

public record StartJoinHabitWorkflowCommand(Guid UserId, Guid HabitId);

public class SagaService
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IHabitJoiningRepository _joiningRepo;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SagaService> _logger;

    public SagaService(
        IWorkflowRepository workflowRepo,
        IHabitJoiningRepository joiningRepo,
        HttpClient httpClient,
        ILogger<SagaService> logger)
    {
        _workflowRepo = workflowRepo;
        _joiningRepo = joiningRepo;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WorkflowInstance> StartJoinHabitAsync(StartJoinHabitWorkflowCommand cmd, CancellationToken ct = default)
    {
        var workflow = new WorkflowInstance(WorkflowType.JoinHabit, cmd.UserId, cmd.HabitId);
        await _workflowRepo.AddAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId} started for user {UserId} joining habit {HabitId}",
            workflow.WorkflowId, cmd.UserId, cmd.HabitId);

        try
        {
            await ExecuteStep1_ValidateUserAsync(workflow, ct);
            await ExecuteStep2_ValidateHabitAsync(workflow, ct);
            await ExecuteStep3_CreateJoiningAsync(workflow, ct);
            await ExecuteStep4_SendNotificationAsync(workflow, ct);
            await ExecuteStep5_CompleteAsync(workflow, ct);
        }
        catch (WorkflowStepException ex)
        {
            _logger.LogError(ex, "Workflow {WorkflowId} failed at step {Step}", workflow.WorkflowId, ex.Step);
            await CompensateAsync(workflow, ex.Step, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow {WorkflowId} failed with unexpected error", workflow.WorkflowId);
            workflow.SetError(ex.Message);
            workflow.TransitionTo(WorkflowState.Failed);
            await _workflowRepo.UpdateAsync(workflow, ct);
        }

        return workflow;
    }

    private async Task ExecuteStep1_ValidateUserAsync(WorkflowInstance workflow, CancellationToken ct)
    {
        _logger.LogInformation("Workflow {WorkflowId}: Step 1 - Validating user {UserId}",
            workflow.WorkflowId, workflow.UserId);

        var response = await _httpClient.GetAsync($"/users/{workflow.UserId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new WorkflowStepException("Step1", $"User {workflow.UserId} not found");

        if (!response.IsSuccessStatusCode)
            throw new WorkflowStepException("Step1", $"UsersService returned {(int)response.StatusCode}");

        workflow.TransitionTo(WorkflowState.UserValidated);
        await _workflowRepo.UpdateAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId}: User validated", workflow.WorkflowId);
    }

    private async Task ExecuteStep2_ValidateHabitAsync(WorkflowInstance workflow, CancellationToken ct)
    {
        _logger.LogInformation("Workflow {WorkflowId}: Step 2 - Validating habit {HabitId}",
            workflow.WorkflowId, workflow.HabitId);

        var response = await _httpClient.GetAsync($"/core-items/{workflow.HabitId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new WorkflowStepException("Step2", $"Habit {workflow.HabitId} not found");

        if (!response.IsSuccessStatusCode)
            throw new WorkflowStepException("Step2", $"Core API returned {(int)response.StatusCode}");

        workflow.TransitionTo(WorkflowState.HabitValidated);
        await _workflowRepo.UpdateAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId}: Habit validated", workflow.WorkflowId);
    }

    private async Task ExecuteStep3_CreateJoiningAsync(WorkflowInstance workflow, CancellationToken ct)
    {
        _logger.LogInformation("Workflow {WorkflowId}: Step 3 - Creating joining record", workflow.WorkflowId);

        var joining = new HabitJoining(workflow.UserId, workflow.HabitId);
        await _joiningRepo.AddAsync(joining, ct);
        workflow.SetJoiningId(joining.Id);
        workflow.TransitionTo(WorkflowState.JoiningCreated);
        await _workflowRepo.UpdateAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId}: Joining {JoiningId} created", workflow.WorkflowId, joining.Id);
    }

    private async Task ExecuteStep4_SendNotificationAsync(WorkflowInstance workflow, CancellationToken ct)
    {
        _logger.LogInformation("Workflow {WorkflowId}: Step 4 - Sending notification", workflow.WorkflowId);

        var notificationPayload = new
        {
            UserId = workflow.UserId,
            HabitId = workflow.HabitId,
            Message = $"You have successfully joined habit {workflow.HabitId}"
        };

        var content = JsonContent.Create(notificationPayload);
        var response = await _httpClient.PostAsync("/notifications", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Workflow {WorkflowId}: Notification failed with {Status}, continuing...",
                workflow.WorkflowId, (int)response.StatusCode);
        }

        workflow.TransitionTo(WorkflowState.NotificationSent);
        await _workflowRepo.UpdateAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId}: Notification step completed", workflow.WorkflowId);
    }

    private async Task ExecuteStep5_CompleteAsync(WorkflowInstance workflow, CancellationToken ct)
    {
        workflow.TransitionTo(WorkflowState.Completed);
        await _workflowRepo.UpdateAsync(workflow, ct);
        _logger.LogInformation("Workflow {WorkflowId} completed successfully", workflow.WorkflowId);
    }

    private async Task CompensateAsync(WorkflowInstance workflow, string failedStep, CancellationToken ct)
    {
        _logger.LogInformation("Workflow {WorkflowId}: Starting compensation after failure in {Step}",
            workflow.WorkflowId, failedStep);

        workflow.TransitionTo(WorkflowState.Compensating);
        await _workflowRepo.UpdateAsync(workflow, ct);

        if (workflow.JoiningId.HasValue)
        {
            _logger.LogInformation("Workflow {WorkflowId}: Compensating - cancelling joining {JoiningId}",
                workflow.WorkflowId, workflow.JoiningId.Value);

            var joining = await _joiningRepo.GetByIdAsync(workflow.JoiningId.Value, ct);
            if (joining != null && joining.Status == JoiningStatus.Active)
            {
                joining.Cancel();
                await _joiningRepo.UpdateAsync(joining, ct);
                _logger.LogInformation("Workflow {WorkflowId}: Joining {JoiningId} cancelled",
                    workflow.WorkflowId, joining.Id);
            }
        }

        workflow.SetError($"Failed at {failedStep}");
        workflow.TransitionTo(WorkflowState.Compensated);
        await _workflowRepo.UpdateAsync(workflow, ct);

        _logger.LogInformation("Workflow {WorkflowId}: Compensation completed", workflow.WorkflowId);
    }
}

public class WorkflowStepException : Exception
{
    public string Step { get; }

    public WorkflowStepException(string step, string message) : base(message)
    {
        Step = step;
    }
}
