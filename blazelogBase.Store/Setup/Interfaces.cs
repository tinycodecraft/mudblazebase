using blazelogBase.Store.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Store.Setup
{
    public interface IBlazeLogDbContext 
    {
        DbSet<CoreSetting> CoreSettings { get; set; }

        DbSet<CoreUser> CoreUsers { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
