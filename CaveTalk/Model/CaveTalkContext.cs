using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace CaveTube.CaveTalk.Model {
	public sealed class CaveTalkContext : DbContext {
		public DbSet<Listener> Listener { get; set; }
		public DbSet<Room> Rooms { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<Account> Account { get; set; }
		public DbSet<Config> Config { get; set; }
	}
}
