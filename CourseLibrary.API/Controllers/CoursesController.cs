using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors/{authorId}/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseLibraryRepository _repo;
    private readonly IMapper _mapper;
    private readonly IValidator<CourseForUpdateDto> _courseForUpdateValidator;

    public CoursesController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IValidator<CourseForUpdateDto> courseForUpdateValidator
    )
    {
        _repo =
            courseLibraryRepository
            ?? throw new ArgumentNullException(nameof(courseLibraryRepository));

        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        _courseForUpdateValidator =
            courseForUpdateValidator
            ?? throw new ArgumentNullException(nameof(courseForUpdateValidator));
    }

    [HttpGet(Name = "GetCoursesForAuthor")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesForAuthor(Guid authorId)
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var coursesForAuthorFromRepo = await _repo.GetCoursesAsync(authorId);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(coursesForAuthorFromRepo));
    }

    [HttpGet("{courseId}", Name = "GetCourseForAuthor")]
    public async Task<ActionResult<CourseDto>> GetCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseForAuthorFromRepo = await _repo.GetCourseAsync(authorId, courseId);

        if (courseForAuthorFromRepo == null)
        {
            return NotFound();
        }
        return Ok(_mapper.Map<CourseDto>(courseForAuthorFromRepo));
    }

    [HttpPost(Name = "CreateCourseForAuthor")]
    public async Task<ActionResult<CourseDto>> CreateCourseForAuthor(
        Guid authorId,
        CourseForCreationDto course
    )
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseEntity = _mapper.Map<Course>(course);
        _repo.AddCourse(authorId, courseEntity);
        await _repo.SaveAsync();

        var courseToReturn = _mapper.Map<CourseDto>(courseEntity);
        return CreatedAtRoute(
            "GetCourseForAuthor",
            new { authorId, courseId = courseToReturn.Id },
            courseToReturn
        );
    }

    [HttpPut("{courseId}")]
    public async Task<IActionResult> UpdateCourseForAuthor(
        Guid authorId,
        Guid courseId,
        CourseForUpdateDto courseForUpdateDto
    )
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        Course? courseFromDb = await _repo.GetCourseAsync(authorId, courseId);
        if (courseFromDb is null)
        {
            Course courseToAdd = _mapper.Map<Course>(courseForUpdateDto);
            courseToAdd.Id = courseId;

            _repo.AddCourse(authorId, courseToAdd);
            await _repo.SaveAsync();

            CourseDto courseToReturn = _mapper.Map<CourseDto>(courseToAdd);
            return CreatedAtRoute("GetCourseForAuthor", new { authorId, courseId }, courseToReturn);
        }

        _mapper.Map(courseForUpdateDto, courseFromDb);
        _repo.UpdateCourse(courseFromDb);
        await _repo.SaveAsync();

        return NoContent();
    }

    [HttpPatch("{courseId}")]
    public async Task<IActionResult> PartiallyUpdateCourseForAuthor(
        Guid authorId,
        Guid courseId,
        JsonPatchDocument<CourseForUpdateDto> patchDocument
    )
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        Course? courseFromDb = await _repo.GetCourseAsync(authorId, courseId);
        if (courseFromDb == null)
        {
            return await CreateNewCourseAsync(authorId, courseId, patchDocument);
        }

        return await UpdateCourseAsync(courseFromDb, patchDocument);
    }

    private async Task<IActionResult> CreateNewCourseAsync(
        Guid authorId,
        Guid courseId,
        JsonPatchDocument<CourseForUpdateDto> patchDocument
    )
    {
        CourseForUpdateDto courseForUpdateToPatch = new CourseForUpdateDto();
        IActionResult? patchProblem = ApplyPatchDocument(patchDocument, courseForUpdateToPatch);
        if (patchProblem != null)
        {
            return patchProblem;
        }

        IActionResult? validationProblem = await ValidatePatchedCourseAsync(courseForUpdateToPatch);
        if (validationProblem != null)
        {
            return validationProblem;
        }

        Course courseToAdd = _mapper.Map<Course>(courseForUpdateToPatch);
        courseToAdd.Id = courseId;

        _repo.AddCourse(authorId, courseToAdd);
        await _repo.SaveAsync();

        CourseDto courseToReturn = _mapper.Map<CourseDto>(courseToAdd);
        return CreatedAtRoute("GetCourseForAuthor", new { authorId, courseId }, courseToReturn);
    }

    private async Task<IActionResult> UpdateCourseAsync(
        Course courseFromDb,
        JsonPatchDocument<CourseForUpdateDto> patchDocument
    )
    {
        CourseForUpdateDto courseToPatch = _mapper.Map<CourseForUpdateDto>(courseFromDb);
        IActionResult? patchProblem = ApplyPatchDocument(patchDocument, courseToPatch);

        if (patchProblem != null)
        {
            return patchProblem;
        }

        IActionResult? validationProblem = await ValidatePatchedCourseAsync(courseToPatch);
        if (validationProblem != null)
        {
            return validationProblem;
        }

        _mapper.Map(courseToPatch, courseFromDb);

        _repo.UpdateCourse(courseFromDb);
        await _repo.SaveAsync();

        return NoContent();
    }

    private async Task<IActionResult?> ValidatePatchedCourseAsync(CourseForUpdateDto courseToPatch)
    {
        ValidationResult validationResult = await _courseForUpdateValidator.ValidateAsync(
            courseToPatch
        );

        if (!validationResult.IsValid)
        {
            foreach (ValidationFailure failure in validationResult.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        return null;
    }

    private IActionResult? ApplyPatchDocument(
        JsonPatchDocument<CourseForUpdateDto> patchDocument,
        CourseForUpdateDto courseToPatch
    )
    {
        try
        {
            patchDocument.ApplyTo(courseToPatch, ModelState);
        }
        catch (Exception) // in case of patchDocument being in incorrect format
        {
            return Problem(
                detail: "The JSON Patch Document is in incorrect format",
                statusCode: 400,
                instance: HttpContext.Request.Path,
                title: "Bad Request"
            );
        }

        // in case of trying to perform an invalid operation, such removing an unexisting field
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return null;
    }

    [HttpDelete("{courseId}")]
    public async Task<ActionResult> DeleteCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!await _repo.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseForAuthorFromRepo = await _repo.GetCourseAsync(authorId, courseId);

        if (courseForAuthorFromRepo == null)
        {
            return NotFound();
        }

        _repo.DeleteCourse(courseForAuthorFromRepo);
        await _repo.SaveAsync();

        return NoContent();
    }

    public override ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary)
    {
        var options = HttpContext.RequestServices.GetRequiredService<
            IOptions<ApiBehaviorOptions>
        >();

        return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
    }
}
