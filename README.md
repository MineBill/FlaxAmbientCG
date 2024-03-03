> [!NOTE]
> Please remember to donate to [ambientCG](https://ambientcg.com/) if you can!

This is a WIP plugin that integrates ambientCG directly inside Flax, giving you the ability to quickly download new material for free, all thanks to [ambientCG](https://ambientcg.com/).

You can find the window under `Tools > AmbientCG`.

https://github.com/MineBill/FlaxAmbientCG/assets/30367251/a09e64d1-9014-4eca-8f60-04f9553f7d97

## How this works
The plugin will attempt to import selected materials by first downloading and extracing the zip file inside the Cache folder of your project. Then, it will import the textures inside the specified `Import Folder`. Once all textures for a material are imported, a new material instance, based on the `Base Pbr Material` will be created. It's important to note that any changes your make to a material instance or texture will be deleted if you re-download that same material again.

## Options
![image](https://github.com/MineBill/FlaxAmbientCG/assets/30367251/1f573645-2414-436f-a89b-4d9dd2da3343)

This following options are exposed under `Tools > Options > AmbientCG`:

| Name  | Description |
| ------------- | ------------- |
| Base Pbr Material  | This is the base material that will be used to create new material instances when downloading new textures. This plugin provides a base one but you can change it to your own.  |
| Import Folder  | This is the folder which new textures/materials will be imported in. Each new material will be placed in it's own folder alongside it's textures. |
| Material Paramter Names | If you decide to use your own material, then you need to have some parameters exposed. For every texture type, you need to have a corresponding parameter with a custom name. You can see the default names above. Keep in mind that, some materials might be missing some textures like Ambient Occlusion or displacement. |

### Custom material
TODO docs
