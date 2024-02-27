using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController, Route("api/authors-collections")]
public class AuthorsCollectionsController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly ICourseLibraryRepository _repository;

    public AuthorsCollectionsController(IMapper mapper, ICourseLibraryRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    [HttpGet("({authorsIds})", Name = "GetAuthorsCollection")]
    public async Task<IActionResult> GetAuthorsCollection(
        [ModelBinder<ArrayModelBinder>] [FromRoute] IEnumerable<Guid>? authorsIds
    )
    {
        if (authorsIds is null)
        {
            return NotFound();
        }

        IEnumerable<Author> authors = await _repository.GetAuthorsAsync(authorsIds);
        if (!authors.Any())
        {
            return NotFound();
        }

        IEnumerable<AuthorDto> authorsToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authors);

        return Ok(authorsToReturn);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAuthorsCollection(
        IEnumerable<AuthorForCreationDto> authorForCreationDtos
    )
    {
        IEnumerable<Author> authorsToAdd = _mapper.Map<IEnumerable<Author>>(authorForCreationDtos);

        foreach (var author in authorsToAdd)
        {
            _repository.AddAuthor(author);
        }

        await _repository.SaveAsync();

        IEnumerable<AuthorDto> authorsToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorsToAdd);
        string authorsIds = string.Join(',', authorsToReturn.Select(a => a.Id));
        return CreatedAtRoute("GetAuthorsCollection", new { authorsIds }, authorsToReturn);
    }
}
