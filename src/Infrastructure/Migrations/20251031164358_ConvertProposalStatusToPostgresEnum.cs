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
            migrationBuilder.Sql("CREATE TYPE proposal_status AS ENUM ('created', 'approved', 'denied');");


            migrationBuilder.Sql(@"
                ALTER TABLE ""Proposals""
                ALTER COLUMN ""Status"" TYPE text
                USING CASE 
                    WHEN ""Status"" = 0 THEN 'created'
                    WHEN ""Status"" = 1 THEN 'approved'
                    WHEN ""Status"" = 2 THEN 'denied'
                    ELSE 'created'
                END;
            ");

            // Depois converter text para o enum
            migrationBuilder.Sql(@"
                ALTER TABLE ""Proposals""
                ALTER COLUMN ""Status"" TYPE proposal_status
                USING ""Status""::proposal_status;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Proposals""
                ALTER COLUMN ""Status"" TYPE integer
                USING CASE 
                    WHEN ""Status""::text = 'created' THEN 0
                    WHEN ""Status""::text = 'approved' THEN 1
                    WHEN ""Status""::text = 'denied' THEN 2
                    ELSE 0
                END;
            ");

            migrationBuilder.Sql("DROP TYPE IF EXISTS proposal_status;");
        }
    }
}
