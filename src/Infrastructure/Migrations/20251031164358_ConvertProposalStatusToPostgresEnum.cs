using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProposalStatusToPostgresEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:proposal_status.proposal_status", "created,approved,denied");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Proposals",
                type: "proposal_status",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:proposal_status.proposal_status", "created,approved,denied");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Proposals",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "proposal_status");
        }
    }
}
