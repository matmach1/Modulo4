using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riesgos.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ImagenesAdjuntas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImagenesAdjuntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReporteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreArchivo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TipoContenido = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TamanoBytes = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCarga = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesAdjuntas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagenesAdjuntas_Reportes_ReporteId",
                        column: x => x.ReporteId,
                        principalTable: "Reportes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesAdjuntas_ReporteId",
                table: "ImagenesAdjuntas",
                column: "ReporteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagenesAdjuntas");
        }
    }
}
