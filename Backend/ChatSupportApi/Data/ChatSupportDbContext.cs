using Microsoft.EntityFrameworkCore;
using ChatSupportApi.Models;

namespace ChatSupportApi.Data
{
    public class ChatSupportDbContext : DbContext
    {
        public ChatSupportDbContext(DbContextOptions<ChatSupportDbContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Team)
                .WithMany(t => t.Agents)
                .HasForeignKey(a => a.TeamId);

            modelBuilder.Entity<ChatSession>()
                .HasOne(cs => cs.AssignedAgent)
                .WithMany()
                .HasForeignKey(cs => cs.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatSessionId);

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Teams
            modelBuilder.Entity<Team>().HasData(
                new Team { Id = 1, Name = "Team A", Shift = ShiftType.Day, IsOverflow = false },
                new Team { Id = 2, Name = "Team B", Shift = ShiftType.Day, IsOverflow = false },
                new Team { Id = 3, Name = "Team C", Shift = ShiftType.Morning, IsOverflow = false },
                new Team { Id = 4, Name = "Overflow Team", Shift = ShiftType.Day, IsOverflow = true }
            );

            // Seed Agents for Team A: 1x team lead, 2x mid-level, 1x junior
            modelBuilder.Entity<Agent>().HasData(
                new Agent { Id = 1, Name = "Team A Lead", Seniority = Seniority.TeamLead, TeamId = 1, IsActive = true },
                new Agent { Id = 2, Name = "Team A Mid 1", Seniority = Seniority.MidLevel, TeamId = 1, IsActive = true },
                new Agent { Id = 3, Name = "Team A Mid 2", Seniority = Seniority.MidLevel, TeamId = 1, IsActive = true },
                new Agent { Id = 4, Name = "Team A Junior", Seniority = Seniority.Junior, TeamId = 1, IsActive = true }
            );

            // Seed Agents for Team B: 1x senior, 1x mid-level, 2x junior
            modelBuilder.Entity<Agent>().HasData(
                new Agent { Id = 5, Name = "Team B Senior", Seniority = Seniority.Senior, TeamId = 2, IsActive = true },
                new Agent { Id = 6, Name = "Team B Mid", Seniority = Seniority.MidLevel, TeamId = 2, IsActive = true },
                new Agent { Id = 7, Name = "Team B Junior 1", Seniority = Seniority.Junior, TeamId = 2, IsActive = true },
                new Agent { Id = 8, Name = "Team B Junior 2", Seniority = Seniority.Junior, TeamId = 2, IsActive = true }
            );

            // Seed Agents for Team C: 2x mid-level (night shift team)
            modelBuilder.Entity<Agent>().HasData(
                new Agent { Id = 9, Name = "Team C Mid 1", Seniority = Seniority.MidLevel, TeamId = 3, IsActive = true },
                new Agent { Id = 10, Name = "Team C Mid 2", Seniority = Seniority.MidLevel, TeamId = 3, IsActive = true }
            );

            // Seed Overflow Team: x6 considered Junior
            for (int i = 0; i < 6; i++)
            {
                modelBuilder.Entity<Agent>().HasData(
                    new Agent { Id = 11 + i, Name = $"Overflow Agent {i + 1}", Seniority = Seniority.Junior, TeamId = 4, IsActive = false }
                );
            }
        }
    }
}
