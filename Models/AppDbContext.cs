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
    }
    public class MasterMcontext : DbContext{
        public MasterMcontext(DbContextOptions<MasterMcontext> options)
            : base(options)
        {
        }
        public DbSet<MasterM> MasterM { get; set; }
    }

}
