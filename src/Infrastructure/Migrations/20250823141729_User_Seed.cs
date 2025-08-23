using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class User_Seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "DateOfBirth", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "20c5308c-f997-4747-b90c-db3a0e830f81", 0, "acc11141-6c70-4ec5-a33c-f9df6fadd952", null, "user1@example.com", false, false, null, "USER1@EXAMPLE.COM", "USER1", "AQAAAAIAAYagAAAAEE5HvjKPN+CAsg8Wr0rqrgbIMw7AjxT9qhFMLS+ZtCeF1Y3nkOAs00jD0MHtRbYpyQ==", null, false, "e2986ff7-3cb6-4fd9-ad4e-7665c01db1dd", false, "user1" },
                    { "a1fee97a-5b19-46d4-ab61-29608d0e793a", 0, "6d9d8935-0ab4-4b5e-9acf-134788e7989e", null, "user2@example.com", false, false, null, "USER2@EXAMPLE.COM", "USER2", "AQAAAAIAAYagAAAAEMgmC5odpySCRAN03w3ynkzPsOcrg2Y/9Nxu2LKlroYlDlsJxj6ZugR5VcOtEguo6w==", null, false, "64897df1-a13e-40fd-a09b-008cceb3f934", false, "user2" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "20c5308c-f997-4747-b90c-db3a0e830f81");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a1fee97a-5b19-46d4-ab61-29608d0e793a");
        }
    }
}
