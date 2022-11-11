using System;
using System.Collections.Generic;

namespace Dan.Plugin.Brreg.Models 
{
    public class EktepaktInfo
    {
        public Guid LastSaved { get; set; }
        public List<EktepaktModel> Ektepakter { get; set; }
    }
}
