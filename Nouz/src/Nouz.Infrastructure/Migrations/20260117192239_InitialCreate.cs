using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nouz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create FTS5 virtual table for full-text search
            migrationBuilder.Sql("""
                CREATE VIRTUAL TABLE BlockSearch USING fts5(
                    BlockId,
                    NoteId,
                    Content
                );
                """);

            migrationBuilder.CreateTable(
                name: "Notebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notebooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NotebookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "Notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Block",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    NoteId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Block", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Block_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Block_NoteId",
                table: "Block",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_Name",
                table: "Notebooks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_SortOrder",
                table: "Notebooks",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_NotebookId",
                table: "Notes",
                column: "NotebookId");

            // Create triggers to sync Block table with FTS5 BlockSearch table
            migrationBuilder.Sql("""
                CREATE TRIGGER blocks_ai AFTER INSERT ON Block
                BEGIN
                    INSERT INTO BlockSearch (BlockId, NoteId, Content)
                    VALUES (new.Id, new.NoteId, new.Content);
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER blocks_au AFTER UPDATE ON Block
                BEGIN
                    UPDATE BlockSearch
                    SET Content = new.Content
                    WHERE BlockId = new.Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER blocks_ad AFTER DELETE ON Block
                BEGIN
                    DELETE FROM BlockSearch WHERE BlockId = old.Id;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS blocks_ad;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS blocks_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS blocks_ai;");

            migrationBuilder.DropTable(
                name: "Block");

            // Drop FTS5 virtual table
            migrationBuilder.Sql("DROP TABLE IF EXISTS BlockSearch;");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Notebooks");
        }
    }
}
