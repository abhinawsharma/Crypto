using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMCryptoCore.Model;

namespace TMCryptoCore.DAL
{
    public class TMCryptoContext:DbContext
    {
        public TMCryptoContext (DbContextOptions<TMCryptoContext> options):base(options) { }
        public DbSet<ASPSessionStateModel> ASPSessionStates { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ASPSessionStateModel>(builder =>
            {
                builder.HasNoKey();
                builder.ToTable("ASPSessionState");
            }
            );
        }
    }
}
