using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LibraryPro.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<BookLoan> BookLoans { get; set; }
        public DbSet<FinePayment> FinePayments { get; set; }
        public DbSet<LibrarySettings> LibrarySettings { get; set; }
        public DbSet<BookReservation> BookReservations { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ValueComparer for List<string> Genre to eliminate EF Core warning 10620
            var genreComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            // Handle List<string> Genre conversion
            modelBuilder.Entity<Book>()
                .Property(b => b.Genre)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    genreComparer);

            // Configure decimal precision to eliminate EF Core warnings 30000
            modelBuilder.Entity<BookLoan>()
                .Property(bl => bl.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FinePayment>()
                .Property(fp => fp.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LibrarySettings>()
                .Property(ls => ls.DailyFineRate)
                .HasPrecision(18, 2);

            // Configure BookLoan relationships
            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Book)
                .WithMany()
                .HasForeignKey(bl => bl.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Member)
                .WithMany(m => m.Loans)
                .HasForeignKey(bl => bl.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BookReservation relationships
            modelBuilder.Entity<BookReservation>()
                .HasOne(br => br.Book)
                .WithMany()
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookReservation>()
                .HasOne(br => br.Member)
                .WithMany()
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FinePayment relationship
            modelBuilder.Entity<FinePayment>()
                .HasOne(fp => fp.Member)
                .WithMany()
                .HasForeignKey(fp => fp.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
