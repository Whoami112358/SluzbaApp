using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Harmonogram_dane")]
    public class Harmonogram
    {
        [Key]
        [Column("ID_Harmonogramu")]
        public int ID_Harmonogramu { get; set; }

        [Column("Data")]
        public DateTime Data { get; set; }

        [Column("Rodzaj_sluzby")]
        public string RodzajSluzby { get; set; }

        [Column("Przypisany_pododdzial")]
        public int PrzypisanyPododdzial { get; set; }

        // Nawigacja do Pododdziału
        [ForeignKey("PrzypisanyPododdzial")]
        public virtual Pododdzial Pododdzial { get; set; }
    };

