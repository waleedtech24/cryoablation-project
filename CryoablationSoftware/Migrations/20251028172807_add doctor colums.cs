using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryoablationSoftware.Migrations
{
    /// <inheritdoc />
    public partial class adddoctorcolums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardApprovals_MedicalRecordId",
                table: "BoardApprovals");

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("30a6704b-d880-4ee0-9e6b-678586e645bf"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("785d42c2-7752-4449-b15d-98c7fca8142a"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("ddaa4678-ef23-46e6-b5a9-c858aa3d7b3f"));

            migrationBuilder.AddColumn<string>(
                name: "ConsultantDoctor1Name",
                table: "Procedures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultantDoctor2Name",
                table: "Procedures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingEngineerName",
                table: "Procedures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryDoctorName",
                table: "Procedures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("28577a0f-52ce-45c8-9708-056016498734"), null, "Manager", "MANAGER" },
                    { new Guid("ac180376-0833-4ce2-803b-e4db62deabad"), null, "Admin", "ADMIN" },
                    { new Guid("d2a05313-6ab6-4d92-a181-5065eb500e5f"), null, "Super Admin", "SUPER ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardApprovals_MedicalRecordId",
                table: "BoardApprovals",
                column: "MedicalRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardApprovals_MedicalRecordId",
                table: "BoardApprovals");

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("28577a0f-52ce-45c8-9708-056016498734"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("ac180376-0833-4ce2-803b-e4db62deabad"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("d2a05313-6ab6-4d92-a181-5065eb500e5f"));

            migrationBuilder.DropColumn(
                name: "ConsultantDoctor1Name",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "ConsultantDoctor2Name",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "OperatingEngineerName",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "PrimaryDoctorName",
                table: "Procedures");

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("30a6704b-d880-4ee0-9e6b-678586e645bf"), null, "Manager", "MANAGER" },
                    { new Guid("785d42c2-7752-4449-b15d-98c7fca8142a"), null, "Super Admin", "SUPER ADMIN" },
                    { new Guid("ddaa4678-ef23-46e6-b5a9-c858aa3d7b3f"), null, "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardApprovals_MedicalRecordId",
                table: "BoardApprovals",
                column: "MedicalRecordId",
                unique: true);
        }
    }
}
