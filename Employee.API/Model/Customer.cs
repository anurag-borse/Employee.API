using System.ComponentModel.DataAnnotations;

namespace Employee.API.Model
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public string Position { get; set; }


        public int Salary { get; set; }

    }
}
