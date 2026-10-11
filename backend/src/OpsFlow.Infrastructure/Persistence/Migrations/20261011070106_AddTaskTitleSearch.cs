using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTitleSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.Sql("""
                CREATE INDEX "IX_TaskItems_Title_Trgm"
                ON "TaskItems" USING gin (lower("Title") gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_TaskItems_Title_Trgm";""");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
