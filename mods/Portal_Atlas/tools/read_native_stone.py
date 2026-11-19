from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[1]
bundle=Path('C:/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/StreamingAssets/SoftRef/Bundles/c4210710')
env=UnityPy.load(str(bundle))
keys={key.lower():key for key in env.container}
material=env.container[keys['assets/gameelements/pieces/_res/stone/stone_mat.mat']].read()
for key,value in material.m_SavedProperties.m_TexEnvs:
    if key=='_MainTex':
        texture=value.m_Texture.read()
        texture.image.save(ROOT/'native-stone-preview-only.png')
        print('Extracted local-only preview texture:',texture.m_Name)
        break
else:
    texture=env.container[keys['assets/gameelements/pieces/_res/stone/stone.png']].read()
    texture.image.save(ROOT/'native-stone-preview-only.png')
    print('Extracted known native stone texture for local preview')
