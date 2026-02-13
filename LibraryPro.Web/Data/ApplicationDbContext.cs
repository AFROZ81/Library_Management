using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<BookLoan> BookLoans { get; set; }
        public DbSet<FinePayment> FinePayments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // This tells EF Core how to handle the List<string> Genre
            modelBuilder.Entity<Book>()
                .Property(b => b.Genre)
                .HasConversion(
                    v => string.Join(',', v),                // To Database: List -> "Fiction,History"
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() // From Database: String -> List
                );
        }
    }
}