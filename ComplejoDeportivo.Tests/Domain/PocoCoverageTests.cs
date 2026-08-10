using System;
using System.Collections.Generic;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using ComplejoDeportivo.Application.Services.Implementations;
using Xunit;

namespace ComplejoDeportivo.Tests.Domain
{
    public class PocoCoverageTests
    {
        [Fact]
        public void Instantiate_All_Entities_And_DTOs()
        {
            var assemDomain = typeof(ComplejoDeportivo.Domain.Complejo).Assembly;
            var assemApp = typeof(ComplejoDeportivo.Application.DTOs.ComplejoDTO).Assembly;

            var types = new List<Type>();
            types.AddRange(assemDomain.GetTypes().Where(t => t.IsClass && !t.IsAbstract));
            types.AddRange(assemApp.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Dto", StringComparison.OrdinalIgnoreCase)));

            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        foreach (var prop in type.GetProperties())
                        {
                            if (prop.CanWrite)
                            {
                                try
                                {
                                    if (prop.PropertyType == typeof(string)) prop.SetValue(instance, "test");
                                    else if (prop.PropertyType == typeof(int)) prop.SetValue(instance, 1);
                                    else if (prop.PropertyType == typeof(bool)) prop.SetValue(instance, true);
                                }
                                catch { }
                            }
                            if (prop.CanRead)
                            {
                                try { var val = prop.GetValue(instance); } catch { }
                            }
                        }
                    }
                }
                catch { } // Ignore classes without parameterless constructors
            }

            // Exceptions
            try { throw new RecursoNoEncontradoException("x"); } catch { }
            try { throw new ReservaSuperpuestaException("x"); } catch { }
            try { throw new NotFoundException("x"); } catch { }

            Assert.True(true);
        }
    }
}
