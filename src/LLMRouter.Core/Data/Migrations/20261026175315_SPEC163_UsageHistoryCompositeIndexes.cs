using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMRouter.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class SPEC163_UsageHistoryCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_usageHistory_ApiKey_Timestamp",
                table: "usageHistory",
                columns: new[] { "ApiKey", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_usageHistory_Model_Timestamp",
                table: "usageHistory",
                columns: new[] { "Model", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_usageHistory_UserId_Timestamp",
                table: "usageHistory",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usageHistory_ApiKey_Timestamp",
                table: "usageHistory");

            migrationBuilder.DropIndex(
                name: "IX_usageHistory_Model_Timestamp",
                table: "usageHistory");

            migrationBuilder.DropIndex(
                name: "IX_usageHistory_UserId_Timestamp",
                table: "usageHistory");
        }
    }
}
