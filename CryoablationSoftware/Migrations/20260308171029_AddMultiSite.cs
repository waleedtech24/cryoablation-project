using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryoablationSoftware.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("3f0343bd-9839-4937-98f9-01bde1bf435f"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("95cd09a5-b2c0-48fe-b1c7-2f495daf93cb"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("c426b6e7-1226-4920-9084-9d7d1ae6bd47"));

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Procedures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MedicalRecordTumorLocation",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    SiteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SiteCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SitePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SiteEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SiteDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.SiteId);
                });

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("306e827d-e97f-47c3-90bf-4d15fddede57"), null, "Super Admin", "SUPER ADMIN" },
                    { new Guid("9cf11d11-d393-48f9-a8df-9e700de8b5ad"), null, "Manager", "MANAGER" },
                    { new Guid("f38b9b81-1d6c-4971-8592-29a63609e457"), null, "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_SiteId",
                table: "UserProfiles",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_SiteId",
                table: "Procedures",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SiteId",
                table: "Patients",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_SiteId",
                table: "Items",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Sites_SiteId",
                table: "Items",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Sites_SiteId",
                table: "Patients",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Sites_SiteId",
                table: "Procedures",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Sites_SiteId",
                table: "UserProfiles",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Sites_SiteId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Sites_SiteId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Sites_SiteId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Sites_SiteId",
                table: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_SiteId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Procedures_SiteId",
                table: "Procedures");

            migrationBuilder.DropIndex(
                name: "IX_Patients_SiteId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Items_SiteId",
                table: "Items");

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("306e827d-e97f-47c3-90bf-4d15fddede57"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("9cf11d11-d393-48f9-a8df-9e700de8b5ad"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("f38b9b81-1d6c-4971-8592-29a63609e457"));

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "MedicalRecordTumorLocation",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("3f0343bd-9839-4937-98f9-01bde1bf435f"), null, "Super Admin", "SUPER ADMIN" },
                    { new Guid("95cd09a5-b2c0-48fe-b1c7-2f495daf93cb"), null, "Manager", "MANAGER" },
                    { new Guid("c426b6e7-1226-4920-9084-9d7d1ae6bd47"), null, "Admin", "ADMIN" }
                });
        }
    }
}
