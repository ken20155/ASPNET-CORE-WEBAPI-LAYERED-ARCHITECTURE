
using Microsoft.EntityFrameworkCore;

namespace WebApiInterviewStatus.Dbconfig
{
    public class Db1Context : DbContext // logdb
    {
        public Db1Context(DbContextOptions<Db1Context> options) : base(options) { }
    }

    public class Db2Context : DbContext // maindb
    {
        public Db2Context(DbContextOptions<Db2Context> options) : base(options) { }
    }

    public class Db3Context : DbContext // sysdb
    {
        public Db3Context(DbContextOptions<Db3Context> options) : base(options) { }
    }
}
