using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoMerge
{
	public interface IChangesetProvider
	{
		 Task<List<ChangesetViewModel>> GetChangesets(string userLogin);
	}
}