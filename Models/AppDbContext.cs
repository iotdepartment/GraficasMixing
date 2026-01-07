using Microsoft.EntityFrameworkCore;

namespace GraficasMixing.Models
{
    public class GaficadoreTestContext : DbContext
    {
        public GaficadoreTestContext(DbContextOptions<GaficadoreTestContext> options)
            : base(options)
        {
        }

        public DbSet<KneaderM> KneaderM { get; set; }
        public DbSet<Oven1> Oven1 { get; set; }

        public DbSet<Oven2> Oven2 { get; set; }

        public DbSet<Oven3> Oven3 { get; set; }

        public DbSet<Oven4> Oven4 { get; set; }

        public DbSet<Oven5> Oven5 { get; set; }

        public DbSet<Oven6> Oven6 { get; set; }
    }
    public class MasterMcontext : DbContext{
        public MasterMcontext(DbContextOptions<MasterMcontext> options)
            : base(options)
        {
        }
        public DbSet<MasterM> MasterM { get; set; }
    }

}
