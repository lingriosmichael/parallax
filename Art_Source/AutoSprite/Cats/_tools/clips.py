"""Clip prompts for the cat animations. Each prompt gets STYLE_TAIL appended (the developer's fixed line)."""

STYLE_TAIL = ("Static camera, no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, "
              "light and outline of the image. The cat stays the same size in every frame.")

DESCRIPTIONS = {
    "A": "A slender short-haired dark brown cat with amber eyes and a long curling tail, hand-painted, "
         "strict side view facing right. A real four-legged cat, not a person.",
    "B": "A slender white cat with pale blue eyes and a thin dark geometric collar marking, hand-painted, "
         "strict side view facing right. A real four-legged cat, not a person.",
}

IN_PLACE = "The cat stays centred in the frame, moving in place like on a treadmill; the ground does not move. "

CLIPS = {
    "01_idle": {"name": "Idle", "loop": True, "fps": 12, "prompt":
        "The cat stands still on all four paws, facing right, breathing slowly: the chest and belly rise and "
        "fall gently, the tail sways a little, one slow blink. Paws stay planted. Seamless loop: the last "
        "frame matches the first. "},
    "02_run": {"name": "Run", "loop": True, "fps": 16, "prompt":
        IN_PLACE + "The cat runs fast to the right in a full feline gallop: back legs push, body stretches "
        "long, front legs reach far forward, then the body gathers with all paws under it. Ears forward, "
        "tail streams back level. Repeating even stride, seamless loop. "},
    "03_walk": {"name": "Walk", "loop": True, "fps": 12, "prompt":
        IN_PLACE + "The cat walks calmly to the right with a relaxed four-beat cat walk, head level, tail "
        "raised in a soft curve. Repeating even stride, seamless loop. "},
    "04_jump_rise": {"name": "Jump rise", "loop": False, "fps": 16, "prompt":
        "The cat crouches for a moment, then springs up and forward off the ground: back legs extend fully, "
        "front legs tuck up to the chest, body angled upward, tail straight back. Ends in the air still "
        "rising. The cat stays near the centre of the frame. "},
    "05_jump_apex": {"name": "Jump apex", "loop": False, "fps": 12, "prompt":
        "The cat hangs in mid-air at the top of a jump, body level and stretched, front legs reaching forward, "
        "back legs trailing, tail out straight for balance. A tiny float, almost still, then the front paws "
        "start to reach down. No ground under the cat; it stays centred. "},
    "06_jump_fall": {"name": "Jump fall", "loop": True, "fps": 12, "prompt":
        "The cat falls through the air, body tilted slightly nose-down, front legs stretched down and "
        "forward ready to land, back legs trailing, fur and tail fluttering upward from the fall. No ground; "
        "the cat stays centred. Seamless loop. "},
    "07_land": {"name": "Land", "loop": False, "fps": 16, "prompt":
        "The cat lands from a jump: front paws touch down first, the body compresses into a soft crouch as "
        "the back paws land, then it rises back to a normal standing pose facing right. Tail swings down "
        "then up. The cat stays in place. "},
    "08_vine_climb_up": {"name": "Vine climb up", "loop": True, "fps": 12, "prompt":
        "The cat climbs straight up a thick vertical vine, seen from the side: body vertical, head up, "
        "front paws reach up and grip in turn, back legs push. The cat stays centred in the frame while "
        "climbing in place; the vine is a green vertical rope right in front of the cat's belly. Seamless loop. "},
    "09_vine_climb_down": {"name": "Vine climb down", "loop": True, "fps": 12, "prompt":
        "The cat climbs down a thick vertical vine backwards, seen from the side: body vertical, head up, "
        "back paws reach down and grip in turn, front paws follow. The cat stays centred in the frame; "
        "the vine is a green vertical rope in front of the cat's belly. Seamless loop. "},
    "10_vine_hang": {"name": "Vine hang", "loop": True, "fps": 12, "prompt":
        "The cat clings to a thick vertical vine, seen from the side: body vertical, head up, all four paws "
        "gripping, holding still; only small breathing, an ear twitch and a slow tail sway. The cat stays "
        "centred. Seamless loop. "},
    "11_vine_leap": {"name": "Vine leap", "loop": False, "fps": 16, "prompt":
        "The cat, clinging vertically to a thick vine on the left, pushes off with its back legs and leaps "
        "away to the right: the body swings from vertical to horizontal, front legs reach forward, tail "
        "streams back. The vine stays on the left edge. "},
    "12a_fidget_tail": {"name": "Fidget tail flick", "loop": False, "fps": 12, "prompt":
        "The cat stands still on all four paws facing right; only the tail moves: a quick annoyed flick of "
        "the tail tip, twice, then it settles back. Paws and body stay planted. "},
    "12b_fidget_ear": {"name": "Fidget ear twitch", "loop": False, "fps": 12, "prompt":
        "The cat stands still on all four paws facing right; one ear twitches quickly twice and the head "
        "turns a little toward the viewer then back. Paws and body stay planted. "},
    "12c_fidget_sit": {"name": "Sit down", "loop": False, "fps": 12, "prompt":
        "The cat, standing on all four paws facing right, gets bored and sits down on its haunches in "
        "profile: back legs fold, front legs stay straight, tail wraps around the front paws. Ends seated, "
        "still, in side view. "},
    "13a_death_spiked": {"name": "Death spiked", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy, no blood, no wounds: the cat jumps straight up in shock as if it sat on a pin, "
        "fur puffed out, legs stiff and spread, eyes wide, then drops and flops over sideways, dazed, with "
        "its tongue out. The cat stays near the centre. "},
    "13b_death_crushed": {"name": "Death crushed", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy, no blood: the cat is squashed flat from above like a pancake, body flattened wide "
        "and thin against the ground, eyes squeezed shut, tail sticking up and wobbling. Stays flat. The cat "
        "stays centred. "},
    "13c_death_zapped": {"name": "Death zapped", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy, no blood: the cat is zapped by lightning: it goes rigid with every hair standing on "
        "end, frizzy and puffed, flickering bright, then ends frazzled and slightly sooty, standing stiff "
        "with a stunned face and a little smoke. The cat stays centred. "},
    "13d_death_splash": {"name": "Death splash", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy: the cat falls into water and pops back up soaked, fur flattened and dripping, "
        "looking miserable and much skinnier, ears drooping. No water surface at the end, just the soaked "
        "cat. The cat stays centred. "},
    "13e_death_arrow": {"name": "Death arrow", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy, no blood, no wound: a thin arrow flies in from the right and knocks the cat over "
        "backwards; the cat topples onto its back with legs in the air, stiff, eyes as dizzy swirls. The "
        "arrow is not stuck in the cat. The cat stays near the centre. "},
    "13f_death_pit": {"name": "Death pit fall", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy: the cat looks down in surprise, legs scramble in the air, then it falls straight "
        "down, flailing, with a shocked face, getting slightly smaller as it drops. Keep the cat's size "
        "until it drops. "},
    "14_door_enter": {"name": "Enter door", "loop": False, "fps": 12, "prompt":
        "The cat walks to the right, slows, and happily trots into a doorway: it turns slightly away from "
        "the viewer and its body and tail disappear into darkness on the right edge, tail tip last. "},
    "15_respawn": {"name": "Respawn", "loop": False, "fps": 16, "prompt":
        "The cat appears in place: it starts as a soft warm golden glow shaped like the cat, the glow fades "
        "as the cat becomes solid, it lands lightly on all four paws facing right, shakes its fur once and "
        "stands ready. "},
    "16a_menu_idle": {"name": "Menu idle", "loop": True, "fps": 10, "prompt":
        "The cat sits calmly in profile facing right, tail wrapped around its front paws, looking ahead with "
        "quiet confidence; slow breathing, a slow blink, the tail tip curls lazily. Seamless loop. "},
    "16b_celebrate": {"name": "Celebrate", "loop": False, "fps": 14, "prompt":
        "The cat celebrates: it does a happy little hop in place, tail straight up with a curl at the tip, "
        "then stands proud facing right with its chin up and eyes half-closed, pleased with itself. "},
    "16c_fed_up": {"name": "Fed up", "loop": False, "fps": 12, "prompt":
        "The cat is fed up: it flattens its ears, gives a long flat unimpressed stare at the viewer, huffs, "
        "and flops down lying on its belly with its chin on its paws, tail thumping the ground in annoyance. "},
}

# PAX-A08 extras (items 2, 4, 6, 7, 9, 12, 13), added 2026-09-29.
CLIPS.update({
    "17_look_around": {"name": "Look around", "loop": False, "fps": 12, "prompt":
        "The cat stands still on all four paws facing right and looks around with curiosity: the head tilts up, "
        "turns toward the viewer with ears forward, tilts to one side, then turns back to the start pose. Paws "
        "stay planted; only the head, ears and tail move. "},
    "18_turn": {"name": "Turn", "loop": False, "fps": 16, "prompt":
        "The cat, standing facing right, quickly turns around on the spot to face left: a fast pivot on its paws, "
        "the body swings round, the tail sweeps over. Ends standing in strict side view facing left. Quick and "
        "snappy. The cat stays centred. "},
    "19_hard_land": {"name": "Hard land", "loop": False, "fps": 16, "prompt":
        "The cat drops from high above and lands hard: the front paws hit, the legs buckle and the body squashes "
        "low almost to its belly, ears flatten, tail slaps down; then it pushes back up to standing facing right "
        "and gives a small shake. The cat stays centred. "},
    "20_death_generic": {"name": "Death frightened", "loop": False, "fps": 16, "prompt":
        "Cartoon comedy, no blood: the cat gets a huge fright. Its fur puffs out all over, the back arches high, "
        "the tail bristles straight up, ears flat, eyes wide, legs stiff and splayed. It jumps a little, lands in "
        "that frightened arched pose and holds it, trembling. The cat stays centred. "},
    "21_gravity_twist": {"name": "Gravity twist", "loop": False, "fps": 16, "prompt":
        "The cat is in mid-air, no ground. It twists its body like a falling cat righting itself: the front half "
        "rolls first, then the back half follows, legs tuck in then reach out again, tail swings round for "
        "balance. It ends in the same side-view pose it started in. The cat stays centred. "},
    "22_launched": {"name": "Launched", "loop": True, "fps": 12, "prompt":
        "The cat is blasted straight upward by a rush from below: body stretched vertical, head up, ears pinned "
        "back, legs tucked tight to the body, eyes squeezed shut, fur and tail streaming downward from the speed. "
        "Only the cat is shown, no water. It holds this pose with a small tremble. Seamless loop. "},
    "23_dizzy": {"name": "Dizzy", "loop": True, "fps": 12, "prompt":
        "The cat stands facing right, dizzy and wobbly: the head sways in slow small circles, eyes are spirals, "
        "legs a little unsteady, tail droops; three small stars circle above its head. Paws stay in place. "
        "Seamless loop: the last frame matches the first. "},
})
