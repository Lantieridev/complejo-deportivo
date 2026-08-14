using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ComplejoDeportivo.Infrastructure.Repositories
{

	public class TipoCanchaRepository : ITipoCanchaRepository
	{
		private readonly ComplejoDeportivoContext _context;

		public TipoCanchaRepository(ComplejoDeportivoContext context)
		{
			_context = context;
		}

		public async Task<TipoCancha> CreateAsync(TipoCancha tipoCancha)
		{
			_context.TipoCanchas.Add(tipoCancha);
			await _context.SaveChangesAsync();
			return tipoCancha;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var tipoCancha = await _context.TipoCanchas.FindAsync(id);
			if (tipoCancha == null)
			{
				return false;
			}
			_context.TipoCanchas.Remove(tipoCancha);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<IEnumerable<TipoCancha>> GetAllAsync()
		{
			return await _context.TipoCanchas.ToListAsync();
		}

		public async Task<TipoCancha?> GetByIdAsync(int id)
		{
			return await _context.TipoCanchas.FindAsync(id);
		}

		public async Task<bool> UpdateAsync(TipoCancha tipoCancha)
		{
			_context.Entry(tipoCancha).State = EntityState.Modified;
			await _context.SaveChangesAsync();
			return true;
		}
	}
}