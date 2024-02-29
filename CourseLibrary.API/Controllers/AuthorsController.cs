using Ardalis.GuardClauses;
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Helpers.PropertyMapping;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;

namespace CourseLibrary.API.Controllers;

[ApiController, Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly PropertyMappingService _propertyMappingService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        PropertyMappingService propertyMappingService,
        ProblemDetailsFactory problemDetailsFactory
    )
    {
        _courseLibraryRepository =
            courseLibraryRepository
            ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _problemDetailsFactory = Guard.Against.Null(problemDetailsFactory);
        _propertyMappingService = Guard.Against.Null(propertyMappingService);
    }

    [HttpGet(Name = "GetAuthors")]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors(
        [FromQuery] AuthorsParameters authorsParameters
    )
    {
        if (!_propertyMappingService.HasMappingsFor<AuthorDto, Author>(authorsParameters.OrderBy))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    detail: $"orderBy contains incorrect property(s): {authorsParameters.OrderBy}",
                    statusCode: 400,
                    instance: HttpContext.Request.Path,
                    title: "Bad Request"
                )
            );
        }

        var pagedAuthors = await _courseLibraryRepository.GetAuthorsAsync(authorsParameters);

        string? previousPageLink = pagedAuthors.HasPrevious
            ? CreateAuthorsUri(ResourceUriType.PreviousPage, authorsParameters)
            : null;

        string? nextPageLink = pagedAuthors.HasNext
            ? CreateAuthorsUri(ResourceUriType.NextPage, authorsParameters)
            : null;

        var paginationMetadata = new
        {
            pageNumber = pagedAuthors.PageNumber,
            pageSize = pagedAuthors.PageSize,
            totalElements = pagedAuthors.TotalElements,
            totalPages = pagedAuthors.TotalPages,
            previousPageLink,
            nextPageLink
        };

        HttpContext.Response.Headers.Append(
            "Pagination-Metadata",
            JsonConvert.SerializeObject(paginationMetadata)
        );

        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(pagedAuthors));
    }

    private string? CreateAuthorsUri(ResourceUriType uriType, AuthorsParameters authorsParameters)
    {
        int pageNumber = authorsParameters.PageNumber - 1;
        if (uriType == ResourceUriType.NextPage)
        {
            pageNumber = authorsParameters.PageNumber + 1;
        }

        var queryParameters = new
        {
            pageNumber,
            pageSize = authorsParameters.PageSize,
            mainCategory = authorsParameters.MainCategory,
            searchQuery = authorsParameters.SearchQuery,
            orderBy = authorsParameters.OrderBy
        };

        return Url.Link("GetAuthors", queryParameters);
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId)
    {
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor", new { authorId = authorToReturn.Id }, authorToReturn);
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        HttpContext.Response.Headers.Append("Allow", "GET, POST, PUT");
        return Ok();
    }
}
