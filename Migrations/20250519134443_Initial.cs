using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sara_coursework.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Awarded",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardedType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MiddleName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CollectiveId = table.Column<int>(type: "int", nullable: true),
                    CollectiveName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awarded", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Awarded_Awarded_CollectiveId",
                        column: x => x.CollectiveId,
                        principalTable: "Awarded",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AwardReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Awards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Decrees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwardReasonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decrees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decrees_AwardReasons_AwardReasonId",
                        column: x => x.AwardReasonId,
                        principalTable: "AwardReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardedId = table.Column<int>(type: "int", nullable: false),
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    DecreeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardAssignments_Awarded_AwardedId",
                        column: x => x.AwardedId,
                        principalTable: "Awarded",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardAssignments_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardAssignments_Decrees_DecreeId",
                        column: x => x.DecreeId,
                        principalTable: "Decrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwardAssignments_AwardedId",
                table: "AwardAssignments",
                column: "AwardedId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardAssignments_AwardId",
                table: "AwardAssignments",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardAssignments_DecreeId",
                table: "AwardAssignments",
                column: "DecreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Awarded_CollectiveId",
                table: "Awarded",
                column: "CollectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Awarded_CollectiveName",
                table: "Awarded",
                column: "CollectiveName",
                unique: true,
                filter: "[CollectiveName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Awarded_LastName_FirstName_Position",
                table: "Awarded",
                columns: new[] { "LastName", "FirstName", "Position" },
                unique: true,
                filter: "[LastName] IS NOT NULL AND [FirstName] IS NOT NULL AND [Position] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AwardReasons_ReasonName",
                table: "AwardReasons",
                column: "ReasonName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Awards_AwardName",
                table: "Awards",
                column: "AwardName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decrees_AwardReasonId",
                table: "Decrees",
                column: "AwardReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Decrees_Number_Date",
                table: "Decrees",
                columns: new[] { "Number", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwardAssignments");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Awarded");

            migrationBuilder.DropTable(
                name: "Awards");

            migrationBuilder.DropTable(
                name: "Decrees");

            migrationBuilder.DropTable(
                name: "AwardReasons");
        }
    }
}
