using UnityEngine;
#if MM_UGUI2
using TMPro;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring TMP Text Color")]
	public class MMSpringTMPTextColor : MMSpringColorComponent<TMP_Text>
	{
		public override Color TargetColor
		{
			get => Target != null ? Target.color : Color.white;
			set { if (Target != null) Target.color = value; }
		}
	}
}
#endif
