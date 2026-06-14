using EscuelaApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Servicios Blazor ────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ── Base de Datos ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<DatabaseService>();

// ── Export Excel (servicio transversal) ─────────────────────────────────────
builder.Services.AddSingleton<ExcelExportService>();

// ── Catálogos ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<AlumnoService>();
builder.Services.AddScoped<MaestroService>();
builder.Services.AddScoped<MateriaService>();
builder.Services.AddScoped<GrupoService>();

// ── Movimientos ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<InscripcionService>();
builder.Services.AddScoped<CalificacionService>();

// ── Reportes ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ReporteService>();

var app = builder.Build();

// ── Inicializar BD al arrancar ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await db.InicializarAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
