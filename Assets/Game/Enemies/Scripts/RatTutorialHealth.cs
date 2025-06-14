using System.Collections;
using Game.Core.Managers;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Game.Enemies.Scripts
{
    public class RatTutorialHealth : RatHealth
    {
        protected override IEnumerator DelayedReturn()
        {
            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }
    }
}