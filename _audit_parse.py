import struct, re, os
from pathlib import Path

# --- Physics matrix ---
hexm = 'ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffd4ffffffd4ffffffebffffffe4ffffffe3ffffffdcffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff'
data = bytes.fromhex(hexm)
masks = [struct.unpack_from('<I', data, i*4)[0] for i in range(32)]
names = ['Default','TransparentFX','Ignore Raycast','3','Water','UI','6','7','Player','Shield','Threat','PlayerProjectile','EnemyProjectile','Boundary']
name_to_idx = {n:i for i,n in enumerate(names)}
def collides(a,b):
    ia,ib = name_to_idx[a], name_to_idx[b]
    return bool(masks[ia] & (1<<ib))
pairs_enable = [('Player','Threat'),('Player','EnemyProjectile'),('Shield','Threat'),('Shield','EnemyProjectile'),('PlayerProjectile','Threat'),('Threat','Boundary'),('PlayerProjectile','Boundary'),('EnemyProjectile','Boundary')]
pairs_disable = [('Player','Player'),('Player','Shield'),('Player','PlayerProjectile'),('Player','Boundary'),('Shield','Shield'),('Shield','PlayerProjectile'),('Shield','Boundary'),('Threat','Threat'),('Threat','EnemyProjectile'),('PlayerProjectile','PlayerProjectile'),('PlayerProjectile','EnemyProjectile'),('EnemyProjectile','EnemyProjectile'),('Boundary','Boundary')]
print('=== COLLISION MATRIX ===')
for a,b in pairs_enable:
    c = collides(a,b) and collides(b,a)
    print(f'ENABLE {a}<->{b}: {c}')
for a,b in pairs_disable:
    c = (not collides(a,b)) and (not collides(b,a))
    print(f'DISABLE {a}<->{b}: {c} (raw {collides(a,b)}/{collides(b,a)})')
for i in range(8,14):
    bits = [names[j] if j < len(names) and names[j] else str(j) for j in range(32) if masks[i] & (1<<j)]
    print(f'MASK {names[i]}: {bits}')

# --- Scene GameObjects ---
scene = Path(r'Assets/_OperationCrossFire/Scenes/Game.unity').read_text(encoding='utf-8')
# Split into GameObject blocks
blocks = re.split(r'\n--- !u!1 &', scene)
print('\n=== GAMEOBJECTS ===')
gos = []
for b in blocks[1:]:
    fid = b.split('\n',1)[0].strip()
    name = re.search(r'm_Name: (.*)', b)
    layer = re.search(r'm_Layer: (\d+)', b)
    active = re.search(r'm_IsActive: (\d+)', b)
    tag = re.search(r'm_TagString: (.*)', b)
    comps = re.findall(r'- component: \{fileID: (\d+)\}', b)
    # parent from transform later
    gos.append({'id':fid,'name':name.group(1) if name else '?','layer':layer.group(1) if layer else '?','active':active.group(1) if active else '?','tag':tag.group(1) if tag else '?','comps':comps})
    print(f"GO {fid}: {name.group(1) if name else '?'} layer={layer.group(1) if layer else '?'} active={active.group(1) if active else '?'} tag={tag.group(1) if tag else '?'} comps={len(comps)}")

# Find root objects via SceneRoots or Transform parents
print('\n=== TRANSFORMS WITH PARENT 0 (roots-ish) ===')
# Transform blocks !u!4 and !u!224
for m in re.finditer(r'--- !u!(4|224) &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    tid, body = m.group(2), m.group(3)
    father = re.search(r'm_Father: \{fileID: (-?\d+)\}', body)
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
    pos = re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', body)
    scale = re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', body)
    children = re.findall(r'- \{fileID: (\d+)\}', body.split('m_Children:')[1].split('m_Father:')[0]) if 'm_Children:' in body else []
    if father and father.group(1) in ('0','-1') and go:
        goname = next((g['name'] for g in gos if g['id']==go.group(1)), '?')
        print(f"ROOT/PARENT0 Transform {tid} GO={go.group(1)} name={goname} pos={pos.groups() if pos else None} children={len(children)}")

# Orthographic size
ortho = re.search(r'm_OrthographicSize: ([^\n]+)', scene)
ortho_flag = re.search(r'm_Orthographic: (\d+)', scene)
print(f'\nCamera ortho={ortho_flag.group(1) if ortho_flag else "?"} size={ortho.group(1) if ortho else "?"}')

# Rigidbody2D summary
print('\n=== Rigidbody2D ===')
for m in re.finditer(r'--- !u!50 &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    body = m.group(2)
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
    bt = re.search(r'm_BodyType: (\d+)', body)
    cd = re.search(r'm_CollisionDetection: (\d+)', body)
    inter = re.search(r'm_Interpolate: (\d+)', body)
    frz = re.search(r'm_FreezeRotation: (\d+)', body)
    constraints = re.search(r'm_Constraints: (\d+)', body)
    goname = next((g['name'] for g in gos if go and g['id']==go.group(1)), '?')
    print(f'RB {goname}: BodyType={bt.group(1) if bt else "?"} CD={cd.group(1) if cd else "?"} Interp={inter.group(1) if inter else "?"} FreezeRot={frz.group(1) if frz else "?"} Constraints={constraints.group(1) if constraints else "?"}')

# Colliders
print('\n=== Colliders Box/Circle ===')
for ut, label in [('61','Box'),('58','Circle')]:
    for m in re.finditer(rf'--- !u!{ut} &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
        body = m.group(2)
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        trig = re.search(r'm_IsTrigger: (\d+)', body)
        size = re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)\}', body)
        rad = re.search(r'm_Radius: ([^\n]+)', body)
        off = re.search(r'm_Offset: \{x: ([^,]+), y: ([^}]+)\}', body)
        goname = next((g['name'] for g in gos if go and g['id']==go.group(1)), '?')
        print(f'{label} {goname}: trigger={trig.group(1) if trig else "?"} size={size.groups() if size else None} rad={rad.group(1) if rad else None} off={off.groups() if off else None}')

# Canvas
print('\n=== Canvas/Scaler ===')
for m in re.finditer(r'--- !u!223 &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    body = m.group(2)
    print('Canvas renderMode', re.search(r'm_RenderMode: (\d+)', body).group(1))
for m in re.finditer(r'm_UiScaleMode: (\d+).*?m_ReferenceResolution: \{x: ([^,]+), y: ([^}]+)\}.*?m_ScreenMatchMode: (\d+).*?m_MatchWidthOrHeight: ([^\n]+)', scene, re.S):
    print('Scaler', m.groups())

# EventSystem count
print('EventSystem count', len(re.findall(r'm_Name: EventSystem', scene)))
print('Main Camera count', len(re.findall(r'm_Name: Main Camera', scene)))
print('Camera components', len(re.findall(r'--- !u!20 &', scene)))

# Texts
print('\n=== TMP texts ===')
for m in re.finditer(r'm_text: (.*)', scene):
    print(' ', m.group(1)[:80])

# Prefab instances in scene
print('\n=== PrefabInstance count ===', len(re.findall(r'--- !u!1001 &', scene)))
print('Prefab sources:', re.findall(r'm_SourcePrefab: \{fileID: 100100000, guid: ([^,]+)', scene))

# Children hierarchy via transform IDs
print('\n=== Building hierarchy ===')
# map transform id -> (go name, father transform, children transforms, pos, scale, rect info)
transforms = {}
for m in re.finditer(r'--- !u!(4|224) &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    kind, tid, body = m.group(1), m.group(2), m.group(3)
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
    father = re.search(r'm_Father: \{fileID: (-?\d+)\}', body)
    pos = re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', body)
    scale = re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', body)
    children_block = body.split('m_Children:')[1].split('m_Father:')[0] if 'm_Children:' in body else ''
    children = re.findall(r'- \{fileID: (\d+)\}', children_block)
    am = re.search(r'm_AnchorMin: \{x: ([^,]+), y: ([^}]+)\}', body)
    aM = re.search(r'm_AnchorMax: \{x: ([^,]+), y: ([^}]+)\}', body)
    ap = re.search(r'm_AnchoredPosition: \{x: ([^,]+), y: ([^}]+)\}', body)
    sd = re.search(r'm_SizeDelta: \{x: ([^,]+), y: ([^}]+)\}', body)
    goname = next((g['name'] for g in gos if go and g['id']==go.group(1)), '?')
    goinfo = next((g for g in gos if go and g['id']==go.group(1)), {})
    transforms[tid] = {'name':goname,'go':go.group(1) if go else None,'father':father.group(1) if father else None,'children':children,'pos':pos.groups() if pos else None,'scale':scale.groups() if scale else None,'kind':kind,'active':goinfo.get('active'),'layer':goinfo.get('layer'),'anchorMin':am.groups() if am else None,'anchorMax':aM.groups() if aM else None,'anchored':ap.groups() if ap else None,'sizeDelta':sd.groups() if sd else None}

def print_tree(tid, indent=0):
    t = transforms.get(tid)
    if not t: return
    extra = f" L{t['layer']} A{t['active']} pos={t['pos']} sc={t['scale']}"
    if t['kind']=='224':
        extra += f" anch={t['anchored']} sd={t['sizeDelta']} amin={t['anchorMin']} amax={t['anchorMax']}"
    print('  '*indent + f"{t['name']}{extra}")
    for c in t['children']:
        print_tree(c, indent+1)

# Find SceneRoots
sr = re.search(r'SceneRoots:.*?m_Roots:(.*)', scene, re.S)
if sr:
    roots = re.findall(r'- \{fileID: (\d+)\}', sr.group(1))
    print('SceneRoots', roots)
    for r in roots:
        print_tree(r)

# CooldownFill details
print('\n=== CooldownFill / Fill Type ===')
for go in gos:
    if 'Cooldown' in go['name'] or go['name'] in ('CooldownFill',):
        print(go)

# Find Image components on CooldownFill
for m in re.finditer(r'--- !u!114 &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    body = m.group(2)
    if 'm_FillAmount:' in body and 'm_Sprite:' in body:
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        goname = next((g['name'] for g in gos if go and g['id']==go.group(1)), '?')
        if 'Cooldown' in goname or 'Fill' in goname:
            print(goname, 'type', re.search(r'm_Type: (\d+)', body).group(1), 'fillMethod', re.search(r'm_FillMethod: (\d+)', body).group(1), 'fillAmt', re.search(r'm_FillAmount: ([^\n]+)', body).group(1), 'raycast', re.search(r'm_RaycastTarget: (\d+)', body).group(1))

# Button OnClick
print('\n=== Buttons ===')
for m in re.finditer(r'--- !u!114 &(\d+)\n(.*?)(?=\n--- !u!|\Z)', scene, re.S):
    body = m.group(2)
    if 'm_OnClick:' in body:
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        goname = next((g['name'] for g in gos if go and g['id']==go.group(1)), '?')
        # persistent calls
        calls = re.search(r'm_PersistentCalls:\n\s+m_Calls:\s*\[(.*?)\]', body, re.S)
        print(f'Button on {goname}: calls section present, empty={calls.group(1).strip()=="" if calls else "unknown"}')
        if 'm_Calls: []' in body:
            print('  empty calls')
        else:
            # show call count
            print('  calls snippet:', body[body.find('m_OnClick:'):body.find('m_OnClick:')+400])

# Input module
print('\n=== Input modules ===')
for name in ['InputSystemUIInputModule','StandaloneInputModule','EventSystem']:
    print(name, 'count', scene.count(name))

# Check Docs
print('\nDocs exists', Path('Docs').exists())
print('DevDocs ignored in gitignore', '/DevDocs/' in Path('.gitignore').read_text())
