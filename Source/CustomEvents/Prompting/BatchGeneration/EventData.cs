// Severity = how dangerous / disruptive the event is for a typical colony.
//   LOW    - harmless, beneficial, or easy to ignore
//   MEDIUM - real damage or a hassle you must react to, but usually survivable
//   HIGH   - can kill colonists, wreck the base, or collapse food/power if unprepared
using RimTelligence.AIStoryteller;
using RimTelligence.Source.AIStoryteller;
using RimTelligence.Source.DataSnapshots.Triggers;
using RimWorld;
using System.Collections.Generic;
using Verse;
namespace RimTelligence.AIStoryteller
{
    public enum Severity
    {
        LOW,
        MEDIUM,
        HIGH
    }

    public class EventInfo
    {
        public string Description { get; }
        public Severity Severity { get; }

        public List<Trigger> Conditions;
        public EventInfo(string description, Severity severity, List<Trigger> conditions)
        {
            Description = description;
            Severity = severity;
            Conditions = conditions;
        }
        public EventInfo(string description, Severity severity)
        {
            Description = description;
            Severity = severity;
            Conditions = new List<Trigger>();

        }
        public string GetDescription()
        {
            //"name: " + def.label + ", description: " + (EventData.descriptions.TryGetValue(def.label, out EventInfo desc) ? desc.Description : "No description available") + ", severity: " + def.severity
            //return "description: " + Description + ", severity: " + Severity + (Conditions.Empty() ? "" : ", conditions: " + string.Join(", ", Conditions));
            return "description: " + Description + ", severity: " + Severity;

        }
        public string getConditions()
        {
            return Conditions.Empty() ? "" : "Triggers:" + string.Join(", ", Conditions);
        }

    }

    public static class EventData
    {

        public static readonly Dictionary<string, EventInfo> descriptions =
            new Dictionary<string, EventInfo>
            {
            { "flu", new EventInfo("Lowers consciousness, movement, breathing. Needs rest and medicine; can kill weak colonists.", Severity.HIGH) },
            { "plague", new EventInfo("Fast illness; kills within days without rest and good medicine.", Severity.HIGH) },
            { "malaria", new EventInfo("Impaired consciousness and blood filtration, later pain, vomiting. Fatal untreated.", Severity.HIGH) },
            //{ "sleeping sickness", new EventInfo("Slowly lowers consciousness until incapacitated. Curable with medicine and rest.", Severity.MEDIUM) },
            { "fibrous mechanites", new EventInfo("Boosts manipulation, movement, blood flow; constant pain, fast tiredness. Fades in 15-30 days.", Severity.LOW) },
            { "sensory mechanites", new EventInfo("Boosts senses, talking, manipulation; intense pain, fast tiredness. Fades in 15-30 days.", Severity.LOW) },
            //{ "gut worms", new EventInfo("Doubles hunger rate.", Severity.LOW) },
            //{ "muscle parasites", new EventInfo("Heavily reduces movement and manipulation.", Severity.LOW) },
            //{ "flu (animals)", new EventInfo("Colony animals lose health; weak/young may die.", Severity.LOW) },
            //{ "plague (animals)", new EventInfo("Colony animals can die within days if untreated.", Severity.MEDIUM) },
            { "organ decay", new EventInfo("Heart, lung or kidney rots over 30-40 days until destroyed. Needs organ replacement; destroyed heart is fatal.", Severity.HIGH) },
            { "blood rot", new EventInfo("Reduces blood pumping and filtration, causes vomiting, pain. Fatal untreated; medicine holds it back.", Severity.MEDIUM) },
            //{ "gill rot", new EventInfo("Kills ~15% of fish per day for 3-6 days. Only fishing suffers.", Severity.LOW) },
            { "paralytic abasia", new EventInfo("Pod crash brings stranger unable to walk for 30-40 days. Can be rescued and recruited.", Severity.LOW) },
 
            // ---------- Positive / neutral events ----------
            { "resource pod crash", new EventInfo("Free resource stack drops on map.", Severity.LOW) },
            { "psychic soothe", new EventInfo("Temporary mood bonus for one gender.", Severity.LOW) },
            { "self-tame", new EventInfo("Wild animal tames itself and joins.", Severity.LOW) },
            { "ambrosia sprout", new EventInfo("Ambrosia sprouts: valuable mood-boosting drug.", Severity.LOW) },
            { "farm animals wander in", new EventInfo("Tame farm animals join free.", Severity.LOW) },
            { "wanderer join", new EventInfo("Lone wanderer asks to join; accept or decline.", Severity.LOW) },
            //{ "wanderers join", new EventInfo("Wanderer group asks to join; accept or decline.", Severity.LOW,new List<Trigger> { new DetectIfJoined() }) },
            //{ "Wanderers", new EventInfo("Wanderer group asks to join; accept or decline.", Severity.LOW,new List<Trigger> { new DetectIfJoined() }) },
            { "transport pod crash", new EventInfo("Injured stranger crashes in; rescue to recruit.", Severity.LOW) },
            { "thrumbos pass", new EventInfo("Thrumbo herd crosses map. Peaceful unless attacked (hit hard); valuable horn and wool.", Severity.LOW) },
            { "ransom demand", new EventInfo("Faction holds your downed colonist; pay silver or lose them.", Severity.MEDIUM) },
            { "meteorite impact", new EventInfo("Mineable ore chunk. Rarely breaks roofs, damages structures or hits a colonist.", Severity.LOW) },
            { "herd migration", new EventInfo("Animal herd passes through; free meat/leather if hunted.", Severity.LOW) },
            { "wild man wanders in", new EventInfo("Non-hostile feral person; capture and recruit.", Severity.LOW) },
            { "ship chunk drop", new EventInfo("Ship chunks to mine for steel.", Severity.LOW) },
            //{ "orbital trader arrival", new EventInfo("Orbital trader; needs powered comms console.", Severity.LOW) },
            { "trader caravan arrival", new EventInfo("Trader caravan camps nearby.", Severity.LOW) },
            { "visitor group", new EventInfo("Friendly visitors; may trade, then leave.", Severity.LOW) },
            { "traveler group", new EventInfo("Travelers pass through. No effect.", Severity.LOW) },
            { "friendly raid", new EventInfo("Allies help fight attackers, then leave.", Severity.LOW) },
            //{ "man in black", new EventInfo("A mysterious figure appears and may offer a deal.", Severity.LOW) },
            //{ "abasia join", new EventInfo("Paralyzed wanderer asks to join; can't walk 30-40 days unless cured.", Severity.LOW) },
            { "tribute collector caravan arrival", new EventInfo("Imperial collectors ask silver; paying improves Empire standing.", Severity.LOW) },
            //{ "anima tree", new EventInfo("Anima tree sprouts; psycasters regain psyfocus faster meditating nearby.", Severity.LOW) },
            //{ "problem causer", new EventInfo("A troublemaker arrives and causes problems for the colony.", Severity.MEDIUM) },
            { "beggars arrive", new EventInfo("Beggars ask for help; giving earns goodwill or small reward.", Severity.LOW) },
            { "pilgrims arrive", new EventInfo("Pilgrims chat, trade, try to convert colonists, then leave.", Severity.LOW) },
            { "Gauranlen pod sprout", new EventInfo("Harvest 1-2 seeds that grow dryad-producing trees.", Severity.LOW) },
            // TODO: verify exact trigger/behaviour of this event
            { "insect jelly", new EventInfo("Insect jelly stack appears to collect (nutritious, mood boost).", Severity.LOW) },
            { "polux tree", new EventInfo("Absorbs pollution (~1 tile per 10 hours).", Severity.LOW) },
            { "bioluminescent spores", new EventInfo("Glowforest only: +4 mood for all colonists for 8-18 hours.", Severity.LOW) },
            { "boomshroom sprout", new EventInfo("Boomshrooms grow in dark; harvest chemfuel, explode if damaged.", Severity.LOW) },
            { "mechanoid signal", new EventInfo("Quest: mech ship crash drops grav engine and gravlite panels for gravship.", Severity.LOW) },
            { "ancient mechanitor complex", new EventInfo("Quest: mech-guarded complex with dead mechanitor's mechlink to become mechanitor.", Severity.MEDIUM) },
            { "Strange signal", new EventInfo("Quest: places Void Monolith, starts Anomaly storyline.", Severity.LOW) },
            { "harbinger tree", new EventInfo("Eats nearby corpses and raw meat; chop for bioferrite and twisted meat.", Severity.LOW) },
 
            // ---------- Weather / environment ----------
            { "psychic drone", new EventInfo("One gender gets -12 to -40 mood for ~1 day; may cause mental breaks.", Severity.MEDIUM) },
            { "toxic fallout", new EventInfo("2.5-10 days: outdoor toxicity (sickness, death), outdoor plants die. Stay indoors.", Severity.HIGH) },
            { "volcanic winter", new EventInfo("7-40 days: deep cold, no outdoor crop growth; famine risk.", Severity.HIGH) },
            { "heat wave", new EventInfo("1.5-3.5 days: heatstroke outdoors or in poorly cooled rooms.", Severity.MEDIUM) },
            { "cold snap", new EventInfo("1.5-3.5 days: hypothermia/frostbite outdoors, crops die.", Severity.MEDIUM) },
            { "flashstorm", new EventInfo("Lightning repeatedly strikes one area: fires, building damage.", Severity.MEDIUM) },
            //{ "short circuit", new EventInfo("Conduit explodes and burns, destroying part of grid (batteries can explode).", Severity.MEDIUM) },
            { "crop blight", new EventInfo("Kills much of one outdoor crop; greenhouses safe only if blight-free.", Severity.MEDIUM) },
            { "alphabeavers", new EventInfo("Eat trees only (no wood left); threat to wood supply.", Severity.LOW) },
            { "acidic smog", new EventInfo("Half plant growth, -5 mood outdoors, corrodes unprotected items.", Severity.LOW) },
            { "volcanic ash", new EventInfo("Several days: -3 mood and reduced breathing outdoors, crops suffer.", Severity.MEDIUM) },
            //{ "lava emergence", new EventInfo("Lava lake wells up, ignites adjacent flammables, takes days to cool.", Severity.MEDIUM) },
            //{ "lava flow", new EventInfo("Lava flows from map edge over volcanic rock/cooled lava, burning everything.", Severity.MEDIUM) },
            //{ "seasonal flooding", new EventInfo("River floods nearby land for days: destroys plants/flooring, slows movement. Walls block it.", Severity.MEDIUM) },
            { "volcanic debris", new EventInfo("8-12 lava chunks hit unroofed areas, creating fire-starting lava pools.", Severity.MEDIUM) },
            { "initial drought conditions", new EventInfo("Hidden 4-day dry spell; always leads to drought.", Severity.LOW) },
            { "drought", new EventInfo("6-10 days: no rain, crops grow at 30% (not hydroponics).", Severity.MEDIUM) },
            { "orbital debris", new EventInfo("Space maps: 12-16 debris hit unroofed cells, injuring pawns.", Severity.MEDIUM) },
            { "blood rain", new EventInfo("Anomaly, 8-36 hours: outdoors builds blood rage until berserk.", Severity.MEDIUM) },
            { "death pall", new EventInfo("Anomaly, 1-3 days: unroofed non-mech corpses rise as hostile shamblers.", Severity.HIGH) },
            { "unnatural darkness", new EventInfo("Anomaly, weeks: unlit areas harm colonists; entities lurk. Light up base.", Severity.HIGH) },
 
            // ---------- Raids, animal threats and ship parts ----------
            { "enemy raid", new EventInfo("Raiders attack to kill, loot, burn or kidnap. Scales with wealth and population.", Severity.HIGH) },
            { "infestation", new EventInfo("Insects burrow from hive in roofed area; hive keeps spawning until destroyed.", Severity.HIGH) },
            //{ "deep drill infestation", new EventInfo("Insects emerge next to deep drill.", Severity.HIGH) },
            //{ "wastepack infestation", new EventInfo("Biotech: insects burst from toxic wastepacks.", Severity.HIGH) },
            { "manhunter pack", new EventInfo("Animal pack hunts colonists until killed (~24-54 hours).", Severity.HIGH) },
            { "mass animal insanity", new EventInfo("Many animals go berserk, attack until killed/downed.", Severity.HIGH) },
            { "single animal insanity", new EventInfo("One animal goes berserk; dangerous if large.", Severity.MEDIUM) },
            { "ship part crash (defoliator)", new EventInfo("Emitter kills plants in wide radius until destroyed; hostile mechanoids.", Severity.HIGH) },
            { "ship part crash (psychic)", new EventInfo("Emitter lowers one gender's mood until destroyed; hostile mechanoids.", Severity.HIGH) },
            { "mech cluster", new EventInfo("Hostile mech cluster (turrets, mortars, sleeping mechs) drops in.", Severity.HIGH) },
 
            // ---------- Anomaly: obelisks, entities and attacks ----------
            { "twisted obelisk", new EventInfo("Mutates pawns with fleshmass. At 100% activity explodes, wildlife becomes hostile fleshbeasts.", Severity.MEDIUM) },
            { "corrupted obelisk", new EventInfo("Duplicates pawns. Attacking it or 100% activity spawns hostile duplicates.", Severity.MEDIUM) },
            { "warped obelisk", new EventInfo("At 100% activity abducts pawns/animals into hostile Labyrinth.", Severity.MEDIUM) },
            { "nociosphere", new EventInfo("Builds activity, pains nearby pawns; when awake hunts and destroys. Study or destroy early.", Severity.HIGH) },
            { "pit gate", new EventInfo("Keeps releasing hostile creatures; leads to dangerous underground.", Severity.HIGH) },
            { "fleshmass heart", new EventInfo("Grows fleshmass, spawns fleshbeasts until destroyed.", Severity.HIGH) },
            { "creepjoiner join", new EventInfo("Stranger joins with benefit and hidden downside (metalhorror, fleshbeast, psychic agony).", Severity.MEDIUM) },
            { "metalhorror implantation", new EventInfo("Colonist secretly infected; bursts out hostile later. Surgery reveals it.", Severity.HIGH) },
            //{ "mysterious cargo revenant spine", new EventInfo("Revenant spine delivered; contain and study, never leave unattended.", Severity.MEDIUM) },
            { "revenant emergence", new EventInfo("Invisible revenant stalks colony, hypnotizing and attacking.", Severity.HIGH) },
            { "unnatural corpse arrival", new EventInfo("Duplicate corpse of a colonist; awakens in ~11-13 days and kills them unless destroyed first.", Severity.HIGH) },
            { "golden cube arrival", new EventInfo("Indestructible cube hypnotizes colonists, hurting productivity; anger if removed.", Severity.MEDIUM) },
            { "ghoul transport pod crash", new EventInfo("Pod releases hostile ghouls (tough, regenerating melee).", Severity.MEDIUM) },
            // TODO: verify exact effect of this event
            //{ "void curiosity", new EventInfo("Void phenomenon affects a colonist unpredictably.", Severity.MEDIUM) },
            { "shambler swarm", new EventInfo("Large shambler swarm attacks from several directions, breaking walls.", Severity.HIGH) },
            { "shambler assault", new EventInfo("Shamblers attack for ~4-10 hours, breaking walls.", Severity.MEDIUM) },
            { "sightstealer swarm", new EventInfo("Fast sightstealer swarm mauls colonists, threatens eyes.", Severity.HIGH) },
            { "sightstealer arrival", new EventInfo("Single fast sightstealer attacks without warning.", Severity.MEDIUM) },
            { "Ghoul attacking", new EventInfo("Small ghoul group attacks.", Severity.MEDIUM) },
            { "revenant", new EventInfo("Invisible hypnotizing revenant attacks.", Severity.HIGH) },
            { "psychic ritual siege", new EventInfo("Horax cultists ritual to abduct colonist or summon fleshbeasts; harming caster triggers attack.", Severity.HIGH) },
            { "hate chanters", new EventInfo("Cultist crowd chants: mood penalty up to -45 until 10-30% fall, then they attack to the death.", Severity.HIGH) },
            { "frenzied animals", new EventInfo("Frenzied animals attack colonists and creatures.", Severity.HIGH) },
            { "fleshbeast attack", new EventInfo("Raid of only fleshbeasts.", Severity.HIGH) },
            { "gorehulk assault", new EventInfo("Gorehulk (huge, heavily armored fleshbeast) attacks.", Severity.HIGH) },
            { "devourer assault", new EventInfo("Devourers (swallow people whole) attack.", Severity.HIGH) },
            { "chimera assault", new EventInfo("Chimera (large, fast fleshbeast) attacks.", Severity.HIGH) },
            };
    }; }
