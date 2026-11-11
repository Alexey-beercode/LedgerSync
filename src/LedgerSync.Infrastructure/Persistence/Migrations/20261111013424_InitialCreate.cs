using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LedgerSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    documents_received = table.Column<int>(type: "integer", nullable: false),
                    documents_created = table.Column<int>(type: "integer", nullable: false),
                    documents_updated = table.Column<int>(type: "integer", nullable: false),
                    documents_skipped = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    google_sheet_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    google_sheet_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    google_sheet_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    external_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_documents", x => x.id);
                    table.CheckConstraint("ck_external_documents_amount_non_negative", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_external_documents_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_log_errors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_log_errors", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_log_errors_sync_logs_sync_log_id",
                        column: x => x.sync_log_id,
                        principalTable: "sync_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "orders",
                columns: new[] { "id", "client_name", "created_at", "order_number" },
                values: new object[,]
                {
                    { new Guid("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0001"), "Alpha Logistics LLP", new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ORD-001" },
                    { new Guid("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0002"), "Beta Construction LLP", new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ORD-002" },
                    { new Guid("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0003"), "Gamma Retail JSC", new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ORD-003" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_documents_external_id",
                table: "external_documents",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_documents_order_id",
                table: "external_documents",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_documents_order_number",
                table: "external_documents",
                column: "order_number");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_log_errors_sync_log_id",
                table: "sync_log_errors",
                column: "sync_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_logs_started_at",
                table: "sync_logs",
                column: "started_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_documents");

            migrationBuilder.DropTable(
                name: "sync_log_errors");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "sync_logs");
        }
    }
}
