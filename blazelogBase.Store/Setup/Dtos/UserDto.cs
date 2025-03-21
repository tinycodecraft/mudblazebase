using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Store.Dtos;

public class UserDto
{
    
    public int Id { get; set; }


    public string UserId { get; set; }


    public string UserName { get; set; }

    
    public string Person { get; set; }

    
    public string EncPassword { get; set; }

    public bool Disabled { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsReset { get; set; }


    public DateTime? LoginedAt { get; set; }

    
    public DateTime UpdatedAt { get; set; }


    public string UpdatedBy { get; set; }

    
    public DateTime? CreatedAt { get; set; }

    
    public int Level { get; set; }


    public string Post { get; set; }


    public string Tel { get; set; }

   
    public string Email { get; set; }

    public string Division { get; set; }
}