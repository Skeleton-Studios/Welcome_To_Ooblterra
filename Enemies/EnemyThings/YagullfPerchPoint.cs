using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
internal class YagullfPerchPoint : MonoBehaviour {
    
    public MeshRenderer NestMesh;
        
    public void SetNestVisible() {
        NestMesh.enabled = true;
    }
        
}
