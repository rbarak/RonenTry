using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Api.Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<int>(
            name: "OfficeIdSequence",
            schema: "dbo",
            startValue: 111L,
            incrementBy: 1);

        migrationBuilder.CreateTable(
            name: "Offices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OfficeId = table.Column<int>(type: "int", nullable: false,
                    defaultValueSql: "NEXT VALUE FOR dbo.OfficeIdSequence"),
                OfficeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false,
                    defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Offices", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Offices_OfficeId",
            table: "Offices",
            column: "OfficeId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Offices_Phone",
            table: "Offices",
            column: "Phone",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Offices");
        migrationBuilder.DropSequence(name: "OfficeIdSequence", schema: "dbo");
    }
}
