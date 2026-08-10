using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComplejoDeportivo.Infrastructure.Repositories
{
    public class ComplejoRepository : IComplejoRepository
    {
        private readonly ComplejoDeportivoContext _context;

        public ComplejoRepository(ComplejoDeportivoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Complejo>> GetAllAsync(string? searchTerm = null)
        {
            // Incluimos la Dirección
            var query = _context.Complejos.Include(c => c.Direccion).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerTerm = searchTerm.ToLower().Trim();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(lowerTerm) ||
                    c.Direccion.Ciudad.ToLower().Contains(lowerTerm)
                );
            }

            return await query.ToListAsync();
        }

        public async Task<Complejo?> GetByIdAsync(int id)
        {
            // Incluimos la Dirección
            return await _context.Complejos
                .Include(c => c.Direccion)
                .FirstOrDefaultAsync(c => c.ComplejoId == id);
        }

        public async Task<Complejo> CreateAsync(Complejo complejo, Direccion direccion)
        {
            ArgumentNullException.ThrowIfNull(complejo);
            ArgumentNullException.ThrowIfNull(direccion);

            // Solo abrimos una transacción propia si el llamador no dejó una activa
            // (ej. un caller o test que ya envuelve la operación en su propia unidad de trabajo).
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                // 1. Crear la Dirección
                _context.Direccions.Add(direccion);
                await _context.SaveChangesAsync();

                // 2. Asignar el ID de la nueva dirección al complejo
                complejo.DireccionId = direccion.DireccionId;

                // 3. Crear el Complejo
                _context.Complejos.Add(complejo);
                await _context.SaveChangesAsync();

                if (ownsTransaction) await transaction!.CommitAsync();

                return complejo;
            }
            catch (Exception)
            {
                if (ownsTransaction) await transaction!.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        public async Task<bool> UpdateAsync(Complejo complejo, Direccion direccion)
        {
            ArgumentNullException.ThrowIfNull(complejo);
            ArgumentNullException.ThrowIfNull(direccion);

            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                // 1. Actualizar la Dirección
                _context.Entry(direccion).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // 2. Actualizar el Complejo
                complejo.DireccionId = direccion.DireccionId; // Asegurarse que el ID esté asignado
                _context.Entry(complejo).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                if (ownsTransaction) await transaction!.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                if (ownsTransaction) await transaction!.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var complejo = await GetByIdAsync(id);
            if (complejo == null)
            {
                return false;
            }

            // Borrar un complejo requiere borrar su dirección y puede fallar si tiene canchas (Foreign Key).
            // Por simplicidad, se intenta el borrado.
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                _context.Complejos.Remove(complejo);
                await _context.SaveChangesAsync();

                // Borramos también la dirección asociada
                if (complejo.Direccion != null)
                {
                    _context.Direccions.Remove(complejo.Direccion);
                    await _context.SaveChangesAsync();
                }

                if (ownsTransaction) await transaction!.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                if (ownsTransaction) await transaction!.RollbackAsync();
                // Lanzamos un error específico si falla (ej. por tener canchas asociadas)
                throw new InvalidOperationException("No se puede eliminar el complejo. Asegúrese de que no tenga canchas u otros elementos asociados.");
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }
    }
}