using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Students;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? active,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var students = await _studentService.ListAsync(userId, search, active, cancellationToken);
            return Ok(students);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var student = await _studentService.GetByIdAsync(userId, id, cancellationToken);
            return Ok(student);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return NotFound(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll(
        EnrollStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var response = await _studentService.EnrollAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex) when (ex.Code == "PLAN_LIMIT_REACHED")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message, code = ex.Code });
        }
        catch (StudentException ex) when (ex.Code == "EMAIL_ALREADY_REGISTERED")
        {
            return Conflict(new { error = ex.Message, code = ex.Code });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var student = await _studentService.UpdateAsync(userId, id, request, cancellationToken);
            return Ok(student);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var student = await _studentService.DeactivateAsync(userId, id, cancellationToken);
            return Ok(student);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var student = await _studentService.ActivateAsync(userId, id, cancellationToken);
            return Ok(student);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex) when (ex.Code == "PLAN_LIMIT_REACHED")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message, code = ex.Code });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }
}
