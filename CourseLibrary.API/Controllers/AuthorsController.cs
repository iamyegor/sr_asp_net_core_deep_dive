using System.Dynamic;
using Ardalis.GuardClauses;
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Helpers.PropertyMapping;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace CourseLibrary.API.Controllers;

[ApiController, Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly PropertyMappingService _propertyMappingService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly PropertyChecker _propertyChecker;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        PropertyMappingService propertyMappingService,
        ProblemDetailsFactory problemDetailsFactory,
        PropertyChecker propertyChecker
    )
    {
        _courseLibraryRepository =
            courseLibraryRepository
            ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _propertyChecker = Guard.Against.Null(propertyChecker);
        _problemDetailsFactory = Guard.Against.Null(problemDetailsFactory);
        _propertyMappingService = Guard.Against.Null(propertyMappingService);
    }

    [HttpGet(Name = "GetAuthors")]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] AuthorsParameters authorsParameters,
        [FromHeader(Name = "Accept")] string acceptHeaderValue
    )
    {
        if (!_propertyMappingService.HasMappingsFor<AuthorDto, Author>(authorsParameters.OrderBy))
        {
            return BadRequestWithDetail(
                $"orderBy contains incorrect property(s): {authorsParameters.OrderBy}"
            );
        }

        if (!_propertyChecker.HasPropertiesForDataShaping<AuthorDto>(authorsParameters.Fields))
        {
            return BadRequestWithDetail(
                $"Author doesn't have some of the provided fields: {authorsParameters.Fields}"
            );
        }

        var pagedAuthors = await _courseLibraryRepository.GetAuthorsAsync(authorsParameters);

        var paginationMetadata = new
        {
            pageNumber = pagedAuthors.PageNumber,
            pageSize = pagedAuthors.PageSize,
            totalElements = pagedAuthors.TotalElements,
            totalPages = pagedAuthors.TotalPages
        };

        HttpContext.Response.Headers.Append(
            "Pagination-Metadata",
            JsonConvert.SerializeObject(paginationMetadata)
        );

        List<ExpandoObject> shapedAuthors = _mapper
            .Map<IEnumerable<AuthorDto>>(pagedAuthors)
            .ShapeData(authorsParameters.Fields)
            .ToList();

        foreach (var shapedAuthor in shapedAuthors)
        {
            IDictionary<string, object?> authorAsDictionary = shapedAuthor;
            authorAsDictionary.Add(
                "links",
                CreateLinksForAuthor((Guid)authorAsDictionary["Id"]!, null)
            );
        }

        var objectToReturn = new
        {
            value = shapedAuthors,
            links = CreateLinksForAuthors(
                authorsParameters,
                pagedAuthors.HasPrevious,
                pagedAuthors.HasNext
            )
        };

        return Ok(objectToReturn);
    }

    public IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorsParameters authorsParameters,
        bool hasPrevious,
        bool hasNext
    )
    {
        List<LinkDto> links = [];

        string selfUrl = Url.Link("GetAuthors", new { })!;
        links.Add(new LinkDto(selfUrl, "get_authors", "GET"));

        string? nextLink = hasNext
            ? CreateAuthorsUri(ResourceUriType.NextPage, authorsParameters)
            : null;
        links.Add(new LinkDto(nextLink, "next_page", "GET"));

        string? prevLink = hasPrevious
            ? CreateAuthorsUri(ResourceUriType.NextPage, authorsParameters)
            : null;
        links.Add(new LinkDto(prevLink, "previous_page", "GET"));

        return links;
    }

    private ActionResult BadRequestWithDetail(string detail)
    {
        return BadRequest(
            _problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                detail: detail,
                statusCode: 400,
                instance: HttpContext.Request.Path,
                title: "Bad Request"
            )
        );
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
    public async Task<ActionResult<AuthorDto>> GetAuthor(
        Guid authorId,
        [FromQuery] string? fields,
        [FromHeader(Name = "Accept")] string acceptHeaderValue
    )
    {
        if (!MediaTypeHeaderValue.TryParse(acceptHeaderValue, out MediaTypeHeaderValue? mediaType))
        {
            return BadRequestWithDetail(
                $"The provided Accept header: {acceptHeaderValue} can't be parsed"
            );
        }

        if (!_propertyChecker.HasPropertiesForDataShaping<AuthorDto>(fields))
        {
            return BadRequestWithDetail(
                $"Author doesn't have some of the provided fields: {fields}"
            );
        }

        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        IDictionary<string, object?> shapedAuthorWithLinks = _mapper
            .Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields);

        if (mediaType.ToString() == "application/vnd.marvin.hateoas+json")
        {
            shapedAuthorWithLinks.Add("links", CreateLinksForAuthor(authorId, fields));
        }

        return Ok(shapedAuthorWithLinks);
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        Author authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        IDictionary<string, object?> shapedAuthorWithLinks = _mapper
            .Map<AuthorDto>(authorEntity)
            .ShapeData(null);

        shapedAuthorWithLinks.Add("link", CreateLinksForAuthor(authorEntity.Id, null));

        return CreatedAtRoute(
            "GetAuthor",
            new { authorId = authorEntity.Id },
            shapedAuthorWithLinks
        );
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
    {
        List<LinkDto> links = [];

        string selfUrl =
            fields == null
                ? Url.Link("GetAuthor", new { authorId })!
                : Url.Link("GetAuthor", new { authorId, fields })!;

        links.Add(new LinkDto(selfUrl, "self", "GET"));

        string createCourseUrl = Url.Link("CreateCourseForAuthor", new { authorId })!;
        links.Add(new LinkDto(createCourseUrl, "create_course_for_author", "POST"));

        string getCoursesUrl = Url.Link("GetCoursesForAuthor", new { authorId })!;
        links.Add(new LinkDto(getCoursesUrl, "get_courses_for_author", "GET"));

        return links;
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        HttpContext.Response.Headers.Append("Allow", "GET, POST, PUT");
        return Ok();
    }
}
