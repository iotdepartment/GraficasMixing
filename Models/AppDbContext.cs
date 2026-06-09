using Microsoft.EntityFrameworkCore;

namespace GraficasMixing.Models
{
    public class GaficadoreTestContext : DbContext
    {
        public GaficadoreTestContext(DbContextOptions<GaficadoreTestContext> options)
            : base(options)
        {
        }

        // Tablas existentes
        public DbSet<KneaderM> KneaderM { get; set; }
        public DbSet<BatchOff> BatchOff { get; set; }
        public DbSet<Oven1> Oven1 { get; set; }
        public DbSet<Oven2> Oven2 { get; set; }
        public DbSet<Oven3> Oven3 { get; set; }
        public DbSet<Oven4> Oven4 { get; set; }
        public DbSet<Oven5> Oven5 { get; set; }
        public DbSet<Oven6> Oven6 { get; set; }
        public DbSet<ScadaExtrudermaster> ScadaExtrudermaster { get; set; }
        public DbSet<SetPointExtruder> SetPointExtruder { get; set; }
        public DbSet<PUMASTER> PUMASTER { get; set; }

        public DbSet<Estado> Estado { get; set; }
        public DbSet<Extruder> Extruders { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Mandril> Mandriles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Estado>()
                .HasOne(e => e.ExtruderRef)
                .WithMany(x => x.Estados)
                .HasForeignKey(e => e.ExtruderId);

            modelBuilder.Entity<Estado>()
                .HasOne(e => e.EmpleadoRef)
                .WithMany(x => x.Estados)
                .HasForeignKey(e => e.EmpleadoId);

            modelBuilder.Entity<Estado>()
                .HasOne(e => e.MandrilRef)
                .WithMany(x => x.Estados)
                .HasForeignKey(e => e.MandrilId);

            modelBuilder.Entity<PUMASTER>().ToTable("PUMASTER", schema: "Extruder.dbo");
        }
    }

    public class ExtruderContext : DbContext
    {
        public ExtruderContext(DbContextOptions<ExtruderContext> options)
            : base(options)
        {
        }

        // Aquí declaramos la tabla PUMASTER de forma limpia
        public DbSet<PUMASTER> PUMASTER { get; set; }
    }


    public class MasterMcontext : DbContext
    {
        public MasterMcontext(DbContextOptions<MasterMcontext> options)
            : base(options)
        {
        }

        public DbSet<MasterM> MasterM { get; set; }
    }

}