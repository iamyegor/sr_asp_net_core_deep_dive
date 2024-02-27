using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors/{authorId}/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseLibraryRepository _repo;
    private readonly IMapper _mapper;

    public CoursesController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _repo =
            courseLibraryRepository
            ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
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

    [HttpPost]
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
            return NotFound();
        }

        CourseForUpdateDto courseToPatch = _mapper.Map<CourseForUpdateDto>(courseFromDb);
        patchDocument.ApplyTo(courseToPatch);
        _mapper.Map(courseToPatch, courseFromDb);

        _repo.UpdateCourse(courseFromDb);
        await _repo.SaveAsync();

        return NoContent();
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
}
