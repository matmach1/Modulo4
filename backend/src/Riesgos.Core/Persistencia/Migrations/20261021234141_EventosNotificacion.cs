using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riesgos.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class EventosNotificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosNotificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReporteId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<long>(type: "INTEGER", nullable: false),
                    AsuntoBorrador = table.Column<string>(type: "TEXT", nullable: false),
                    CuerpoBorrador = table.Column<string>(type: "TEXT", nullable: false),
                    FechaActualizacion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosNotificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosNotificacion_Reportes_ReporteId",
                        column: x => x.ReporteId,
                        principalTable: "Reportes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosNotificacion_ReporteId",
                table: "EventosNotificacion",
                column: "ReporteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosNotificacion");
        }
    }
}
