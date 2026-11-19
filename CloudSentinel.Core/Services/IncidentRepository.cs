using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Models;

namespace CloudSentinel.Core.Services
{
    public class IncidentRepository
    {
        private readonly List<Incident> _incidents = [];
        
        public void Add(Incident incident) { _incidents.Add(incident); }
        public List<Incident> GetAll()
        { return _incidents.OrderByDescending(x => x.CreatedAt).ToList(); }
    }
}
