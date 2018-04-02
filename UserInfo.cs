using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FameBase
{
	//public class UserInfo
	//{
	//	public string Name { get; set; }

	//	//public Stat CameraA { get; set; }
	//	//public Stat CameraB { get; set; }
	//	//public Stat MixerA { get; set; }
	//	//public Stat MixerB { get; set; }

	//	public List<Stat> Tutorials { get; set; }
	//}

	//public class Stat
	//{
	//	public string Tutorial { get; set; }
	//	public double time { get; set; }
	//	public int Prev_count { get; set; }
	//	public int Next_count { get; set; }
	//	public int Redo_count { get; set; }
	//}

	public class UserInfo
	{
		public string Name { get; set; }
		public Stat CameraA { get; set; }
		public Stat CameraB { get; set; }
		public Stat MixerA { get; set; }
		public Stat MixerB { get; set; }

	}

	public class Stat
	{
		public float Tutorial_time { get; set; }
		//public List<string> step_time { get; set; }
		public List<Step> Step_time;
		public int Prev_count { get; set; }
		public int Next_count { get; set; }
		public int Redo_count { get; set; }
	}

	public class Step
	{
		public string Way { get; set; }
		public float Time { get; set; }
		public int PageIndex { get; set; }
	}

	
}
