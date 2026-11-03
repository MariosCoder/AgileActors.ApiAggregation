using System.ComponentModel.DataAnnotations;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AgileActors.ApiAggregation.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> Search([FromQuery, Required, StringLength(200)] string query, CancellationToken cancellationToken)
    {
        try
        {
            var books = await _bookService.SearchBooksAsync(query, cancellationToken);

            return Ok(books);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Book search query was rejected.",
                detail: "The book provider could not process this query. Try a more specific title or author.");
        }
    }
}