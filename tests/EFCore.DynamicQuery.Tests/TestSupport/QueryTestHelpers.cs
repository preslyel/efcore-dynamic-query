using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

internal static class QueryTestHelpers
{
    public static QueryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QueryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new QueryTestDbContext(options);
    }

    public static IDynamicMapper CreateMapper() => new DynamicMapper([typeof(BookProfile).Assembly]);

    public static void SeedBooks(QueryTestDbContext context)
    {
        var tolkien = new Author { Id = 1, Name = "J.R.R. Tolkien" };
        var orwell = new Author { Id = 2, Name = "George Orwell" };

        context.Authors.AddRange(tolkien, orwell);

        context.Books.AddRange(
            new Book
            {
                Id = 1,
                Title = "The Hobbit",
                Price = 10m,
                AuthorId = 1,
                Author = tolkien,
                Reviews = [new Review { Id = 1, Rating = 4 }, new Review { Id = 2, Rating = 5 }]
            },
            new Book
            {
                Id = 2,
                Title = "The Fellowship of the Ring",
                Price = 12m,
                AuthorId = 1,
                Author = tolkien,
                Reviews = [new Review { Id = 3, Rating = 3 }]
            },
            new Book
            {
                Id = 3,
                Title = "1984",
                Price = 8m,
                AuthorId = 2,
                Author = orwell,
                Reviews = [new Review { Id = 4, Rating = 5 }, new Review { Id = 5, Rating = 1 }]
            },
            new Book
            {
                Id = 4,
                Title = "Animal Farm",
                Price = 6m,
                AuthorId = 2,
                Author = orwell,
                Reviews = []
            });

        context.SaveChanges();
    }
}
