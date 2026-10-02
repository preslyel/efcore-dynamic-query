using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public List<Review> Reviews { get; set; } = [];
}

public class Review
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int Rating { get; set; }
}

public class BookModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public string AuthorName { get; set; } = "";
    public IEnumerable<int> ReviewRatings { get; set; } = [];
    public string ReviewRatingsJoined { get; set; } = "";
}

public class BookProfile : MappingProfile
{
    public BookProfile()
    {
        CreateMap<Book, BookModel>()
            .ForMember(dest => dest.AuthorName, src => src.Author.Name)
            .ForMember(dest => dest.ReviewRatings, src => src.Reviews.Select(r => r.Rating))
            .ForMember(dest => dest.ReviewRatingsJoined, src => string.Join(", ", src.Reviews.Select(r => r.Rating.ToString())))
            .ForMember(dest => dest.Price, src => src.Price, cfg => cfg.MutableFunction = p => p * 2m);
    }
}

public sealed class QueryTestDbContext(DbContextOptions<QueryTestDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Review> Reviews => Set<Review>();
}
