using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public struct Cell
    {
        public int i;
        public int j;
        public HashSet<GameObject> gameobjects;
        public Cell(int i, int j, HashSet<GameObject> gameobjects)
        {
            this.i = i;
            this.j = j;
            this.gameobjects = gameobjects;
        }
    }

    public partial class Level
    {
        public void setupGrid(float cellWidth)
        {
            this.cellWidth = cellWidth;
            var width = this.width;
            var height = this.height;
            var hCellCount = Math.Ceiling(width / cellWidth);
            var vCellCount = Math.Ceiling(height / cellWidth);
            //console.log("Creating grid with width " + hCellCount + " and height " + vCellCount);
            for (var i = 0; i < vCellCount; i++)
            {
                var curRow = new List<HashSet<GameObject>>();
                grid.Add(curRow);
                for (var j = 0; j < hCellCount; j++)
                {
                    curRow.Add(new HashSet<GameObject>());
                }
            }
        }

        //Optimize this function, it will be called a lot
        public List<Cell> getGridCells(Shape shape)
        {
            var cells = new List<Cell>();

            //Line case
            if (shape.points.Count == 2)
            {
                var point1 = shape.points[0];
                var point2 = shape.points[1];
                var dir = point1.directionToNorm(point2);
                var curX = point1.x;
                var curY = point1.y;
                float dist = 0;
                var maxDist = point1.distanceTo(point2);
                //var mag = maxDist / (this.cellWidth/2);
                float mag = cellWidth / 2;
                HashSet<int> usedCoords = new HashSet<int>();
                while (dist < maxDist)
                {
                    int i = MathF.Floor((curY / height) * grid.Count);
                    int j = MathF.Floor((curX / width) * grid[0].Count);
                    curX += dir.x * mag;
                    curY += dir.y * mag;
                    dist += mag;
                    if (i < 0 || j < 0 || i >= grid.Count || j >= grid[0].Count) continue;
                    int gridCoordKey = Helpers.getGridCoordKey((ushort)i, (ushort)j);
                    if (usedCoords.Contains(gridCoordKey)) continue;
                    usedCoords.Add(gridCoordKey);
                    cells.Add(new Cell(i, j, grid[i][j]));
                }
                return cells;
            }

            int minI = Helpers.clamp(MathF.Floor((shape.minY / height) * grid.Count), 0, grid.Count - 1);
            int minJ = Helpers.clamp(MathF.Floor((shape.minX / width) * grid[0].Count), 0, grid[0].Count - 1);
            int maxI = Helpers.clamp(MathF.Floor((shape.maxY / height) * grid.Count), 0, grid.Count - 1);
            int maxJ = Helpers.clamp(MathF.Floor((shape.maxX / width) * grid[0].Count), 0, grid[0].Count - 1);

            for (int i = minI; i <= maxI; i++)
            {
                for (int j = minJ; j <= maxJ; j++)
                {
                    if (i < 0 || j < 0 || i >= grid.Count || j >= grid[0].Count) continue;
                    cells.Add(new Cell(i, j, grid[i][j]));
                }
            }
            return cells;
        }

        //Called a lot
        public List<GameObject> getGameObjectsInSameCell(Shape shape)
        {
            var cells = getGridCells(shape);
            var retGameobjects = new HashSet<GameObject>();
            foreach (var cell in cells)
            {
                if (cell.gameobjects == null) continue;
                foreach (var cell2 in cell.gameobjects)
                {
                    if (gameObjects.Contains(cell2))
                    {
                        retGameobjects.Add(cell2);
                    }
                    else
                    {
                        gameObjects.Remove(cell2);
                        //console.log(cell2);
                        //throw "A gameobject was found in a cell but no longer exists in the map";
                    }
                }
            }
            var arr = new List<GameObject>();
            foreach (var go in retGameobjects)
            {
                arr.Add(go);
            }
            return arr;
        }

        // Should be called when the object is destroyed for thorough cleanup.
        public void removeFromGrid(GameObject go)
        {
            foreach (var gridSet in occupiedGridSets)
            {
                if (gridSet.Contains(go))
                {
                    gridSet.Remove(go);
                }
                if (gridSet.Count == 0)
                {
                    occupiedGridSets.Remove(gridSet);
                }
            }
        }

        // Should be called on hitbox changes.
        public void removeFromGridFast(GameObject go)
        {
            Shape? allCollidersShape = go.getAllCollidersShape();
            if (allCollidersShape == null) return;
            var cells = getGridCells(allCollidersShape.Value);
            foreach (var cell in cells)
            {
                if (cell.gameobjects.Contains(go))
                {
                    cell.gameobjects.Remove(go);
                }
            }
        }

        public void addGameObjectToGrid(GameObject go)
        {
            if (!gameObjects.Contains(go)) return;
            Shape? allCollidersShape = go.getAllCollidersShape();
            if (allCollidersShape == null) return;
            var cells = getGridCells(allCollidersShape.Value);
            foreach (var cell in cells)
            {
                if (grid.InRange(cell.i) && grid[cell.i].InRange(cell.j) && !grid[cell.i][cell.j].Contains(go))
                {
                    grid[cell.i][cell.j].Add(go);
                    occupiedGridSets.Add(grid[cell.i][cell.j]);
                }
            }
        }

        public Point getGroundPos(Point pos, float depth = 60)
        {
            var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
            if (hit == null) return pos;
            return hit.hitData.hitPoint.Value.addxy(0, -1);
        }

        public Point? getGroundPosNoKillzone(Point pos, float depth = 60)
        {
            var kzHit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(KillZone) });
            if (kzHit != null) return null;

            var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
            if (hit == null) return null;

            return hit.hitData.hitPoint.Value.addxy(0, -1);
        }

        public Point? getGroundPosWithNull(Point pos, float depth = 60)
        {
            var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
            if (hit == null) return null;
            return hit.hitData.hitPoint.Value.addxy(0, -1);
        }

        public int getGridCount()
        {
            int gridItemCount = 0;
            for (int i = 0; i < grid.Count; i++)
            {
                for (int j = 0; j < grid[i].Count; j++)
                {
                    if (grid[i][j].Count > 0)
                    {
                        gridItemCount += grid[i][j].Count;
                    }
                }
            }
            return gridItemCount;
        }

        public void getTotalCountInGrid()
        {
            var count = 0;
            var orphanedCount = 0;
            var width = this.width;
            var height = this.height;
            var hCellCount = Math.Ceiling(width / cellWidth);
            var vCellCount = Math.Ceiling(height / cellWidth);
            for (var i = 0; i < vCellCount; i++)
            {
                for (var j = 0; j < hCellCount; j++)
                {
                    count += grid[i][j].Count;
                    var set = grid[i][j];
                    foreach (var go in set)
                    {
                        if (!gameObjects.Contains(go))
                        {
                            //this.grid[i][j].delete(go);
                            orphanedCount++;
                        }
                    }
                }
            }
            debugString = count.ToString();
            debugString2 = orphanedCount.ToString();
        }

        public bool hasGameObject(GameObject go)
        {
            return gameObjects.Contains(go);
        }

        public void addGameObject(GameObject go)
        {
            gameObjects.Add(go);
            addGameObjectToGrid(go);
        }

        public void removeGameObject(GameObject go)
        {
            removeFromGrid(go);
            gameObjects.Remove(go);
        }

        public List<GameObject> getGameObjectArray()
        {
            return new List<GameObject>(gameObjects);
        }

        //Should actor collide with gameobject?
        //Note: return true to indicate NOT to collide, and instead only trigger
        public bool shouldTrigger(Actor actor, GameObject gameObject, Collider actorCollider, Collider gameObjectCollider, Point intersection, bool otherway = false)
        {
            if (actor is Character && gameObject is Character)
            {
                var actorChar = actor as Character;
                var goChar = gameObject as Character;

                if (actorChar.isCrystalized || goChar.isCrystalized) return false;
                //if (actorChar.sprite.name.Contains("frozen") || goChar.sprite.name.Contains("frozen")) return false;
                return true;
            }

            if (actor is Character chr3 && (chr3.player.isViralSigma() || chr3.player.isKaiserViralSigma()) && gameObject is Ladder)
            {
                return true;
            }

            if (actorCollider.isTrigger == false && gameObject is Ladder)
            {
                if (actor.pos.y < gameObject.collider.shape.getRect().y1 && intersection.y > 0)
                {
                    if (!actor.checkLadderDown)
                    {
                        return false;
                    }
                }
            }

            if (actorCollider.disabled || gameObjectCollider.disabled) return false;
            if (actorCollider.isTrigger || gameObjectCollider.isTrigger) return true;

            if (actor is ShotgunIceProjSled sled && gameObject is Character chr && sled.damager.owner == chr.player)
            {
                return false;
            }

            if (actor is Character chr2 && gameObject is ShotgunIceProjSled sled2 && sled2.damager.owner == chr2.player)
            {
                return false;
            }

            if (actorCollider.wallOnly && gameObject is not Wall) return true;

            if (gameObject is Actor)
            {
                if (gameObjectCollider.wallOnly) return true;
            }

            if (actor is Character && gameObject is RideArmor) return true;
            if (actor is RideArmor && gameObject is Character) return true;
            if (actor is RideArmor && gameObject is RideArmor) return true;

            /*
            if (actor is Character && gameObject is Character && ((Character)actor).player.alliance == ((Character)gameObject).player.alliance) 
            {
                return true;
            }
            if (actor is Character && gameObject is Character && ((Character)actor).player.alliance != ((Character)gameObject).player.alliance && (((Character)actor).isStingCharged || ((Character)gameObject).isStingCharged)) 
            {
                return true;
            }
            if (actor is Character && gameObject is Character && ((Character)actor).player.alliance != ((Character)gameObject).player.alliance && (((Character)actor).insideCharacter || ((Character)gameObject).insideCharacter))
            {
                return true;
            }
            */
            var ra = gameObject as RideArmor;
            if (actor is ShotgunIceProjSled && ra != null && (ra.character == null || ra.character.player.alliance == (actor as ShotgunIceProjSled).damager.owner.alliance))
            {
                return true;
            }
            if (actor is ShotgunIceProjSled && gameObject is Projectile)
            {
                return true;
            }

            //Must go both ways
            if (gameObject is Actor && !otherway)
            {
                var otherWay = shouldTrigger((Actor)gameObject, actor, gameObjectCollider, actorCollider, intersection.times(-1), true);
                return otherWay;
            }

            return false;
        }

        public Point? getMtvDir(Actor actor, float inX, float inY, Point? vel, bool pushIncline, List<CollideData> overrideCollideDatas = null)
        {
            var collideDatas = overrideCollideDatas;
            if (collideDatas == null)
            {
                collideDatas = Global.level.checkCollisionsActor(actor, inX, inY, vel);
            }

            var onlyWalls = collideDatas.Where(cd => !(cd.gameObject is Wall)).Count() == 0;
            //var onlyWalls = (_.filter(collideDatas, (cd) => { return !(cd.gameObject is Wall); })).Count == 0;

            var actorShape = actor.collider.shape.clone(inX, inY);
            Point? pushDir = null;

            if (vel != null)
            {
                pushDir = vel?.times(-1).normalize();
                if (collideDatas.Count > 0)
                {
                    foreach (var collideData in collideDatas)
                    {
                        if (collideData.hitData != null && collideData.hitData.normal != null && ((Point)collideData.hitData.normal).isAngled() && pushIncline && onlyWalls)
                        {
                            pushDir = new Point(0, -1); //Helpers.getInclinePushDir(collideData.hitData.normal, vel);
                        }
                    }
                }
            }

            if (collideDatas.Count > 0)
            {
                float maxMag = 0;
                Point? maxMtv = null;
                foreach (var collideData in collideDatas)
                {
                    actor.registerCollision(collideData);

                    Point? mtv = pushDir == null ?
                        actorShape.getMinTransVector(collideData.otherCollider.shape) :
                        actorShape.getMinTransVectorDir(collideData.otherCollider.shape, (Point)pushDir);

                    if (mtv != null && ((Point)mtv).magnitude >= maxMag)
                    {
                        maxMag = ((Point)mtv).magnitude;
                        maxMtv = ((Point)mtv);
                    }
                }
                return maxMtv;
            }
            else
            {
                return null;
            }
        }

        public CollideData checkCollisionPoint(Point point, List<GameObject> exclusions)
        {
            var points = new List<Point>();
            points.Add(point);
            points.Add(point.addxy(1, 0));
            points.Add(point.addxy(1, 1));
            points.Add(point.addxy(0, 1));
            Shape shape = new Shape(points);
            return checkCollisionShape(shape, exclusions);
        }

        public CollideData checkCollisionShape(Shape shape, List<GameObject> exclusions)
        {
            var gameObjects = getGameObjectsInSameCell(shape);
            foreach (var go in gameObjects)
            {
                if (go.collider == null) continue;
                if (go is not Actor && go.collider.isTrigger) continue;
                if (go is Actor && (go.collider.isTrigger || go.collider.wallOnly)) continue;
                if (exclusions != null && exclusions.Contains(go)) continue;
                var hitData = shape.intersectsShape(go.collider.shape);
                if (hitData != null)
                {
                    return new CollideData(null, go.collider, null, false, go, hitData);
                }
            }
            return null;
        }

        public List<CollideData> checkCollisionsShape(Shape shape, List<GameObject> exclusions)
        {
            var hitDatas = new List<CollideData>();
            var gameObjects = getGameObjectsInSameCell(shape);
            foreach (var go in gameObjects)
            {
                if (go.collider == null) continue;
                if (exclusions != null && exclusions.Contains(go)) continue;
                var hitData = shape.intersectsShape(go.collider.shape);
                if (hitData != null)
                {
                    hitDatas.Add(new CollideData(null, go.collider, null, false, go, hitData));
                }
            }

            return hitDatas;
        }

        // Checks for collisions and returns the first one collided.
        // A collision requires at least one of the colliders not to be a trigger.
        // The vel parameter ensures we return normals that make sense, that are against the direction of vel.
        public CollideData checkCollisionActor(Actor actor, float incX, float incY, Point? vel = null, bool autoVel = false, bool checkPlatforms = false)
        {
            return checkCollisionsActor(actor, incX, incY, vel, autoVel, returnOne: true, checkPlatforms: checkPlatforms).FirstOrDefault();
        }

        public List<CollideData> checkCollisionsActor(Actor actor, float incX, float incY, Point? vel = null, bool autoVel = false, bool returnOne = false, bool checkPlatforms = false)
        {
            var collideDatas = new List<CollideData>();
            if (actor.collider == null) return collideDatas;

            if (autoVel && vel == null) vel = new Point(incX, incY);
            var actorShape = actor.collider.shape.clone(incX, incY);
            var gameObjects = getGameObjectsInSameCell(actorShape);
            foreach (var go in gameObjects)
            {
                if (go == actor) continue;
                if (go.collider == null) continue;
                var isTrigger = shouldTrigger(actor, go, actor.collider, go.collider, new Point(incX, incY));
                if (go is Actor goActor && goActor.isPlatform && checkPlatforms)
                {
                    isTrigger = false;
                }
                if (isTrigger) continue;
                var hitData = actorShape.intersectsShape(go.collider.shape, vel);
                if (hitData != null)
                {
                    collideDatas.Add(new CollideData(actor.collider, go.collider, vel, isTrigger, go, hitData));
                    if (returnOne)
                    {
                        return collideDatas;
                    }
                }
            }

            return collideDatas;
        }

        public List<CollideData> getTriggerList(Actor actor, float incX, float incY, Point? vel = null, params Type[] classTypes)
        {
            var triggers = new List<CollideData>();
            var myColliders = actor.getAllColliders();
            if (myColliders.Count == 0) return triggers;

            foreach (var myCollider in myColliders)
            {
                var myActorShape = myCollider.shape.clone(incX, incY);
                var gameObjects = getGameObjectsInSameCell(myActorShape);
                foreach (var go in gameObjects)
                {
                    if (go == actor) continue;
                    if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) continue;
                    var otherColliders = go.getAllColliders();
                    if (otherColliders.Count == 0) continue;

                    foreach (Collider otherCollider in otherColliders)
                    {
                        var isTrigger = shouldTrigger(actor, go, myCollider, otherCollider, new Point(incX, incY));
                        if (!isTrigger) continue;
                        var hitData = myActorShape.intersectsShape(otherCollider.shape, vel);
                        if (hitData != null)
                        {
                            triggers.Add(new CollideData(myCollider, otherCollider, vel, isTrigger, go, hitData));
                        }
                    }
                }
            }

            return triggers;
        }

        public List<CollideData> getTriggerList(Shape shape, params Type[] classTypes)
        {
            var triggers = new List<CollideData>();
            var gameObjects = getGameObjectsInSameCell(shape);
            foreach (var go in gameObjects)
            {
                if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) continue;
                var otherColliders = go.getAllColliders();
                if (otherColliders.Count == 0) continue;

                foreach (var otherCollider in otherColliders)
                {
                    var isTrigger = otherCollider.isTrigger;
                    if (!isTrigger) continue;
                    var hitData = shape.intersectsShape(otherCollider.shape, null);
                    if (hitData != null)
                    {
                        triggers.Add(new CollideData(null, otherCollider, null, isTrigger, go, hitData));
                    }
                }
            }
            return triggers;
        }

        public bool isOfClass(object go, List<Type> classNames)
        {
            return Helpers.isOfClass(go, classNames);
        }

        public List<CollideData> raycastAll(Point pos1, Point pos2, List<Type> classNames, bool isChargeBeam = false)
        {
            var hits = new List<CollideData>();
            var shape = new Shape(new List<Point>() { pos1, pos2 });
            var gameObjects = getGameObjectsInSameCell(shape);
            foreach (var go in gameObjects)
            {
                if (go.collider == null) continue;
                if (!isOfClass(go, classNames)) continue;
                var goCollider = go.collider;

                // Fix a one-off case where charge beam wouldn't lock onto Kaiser's head
                if (isChargeBeam && go is Character chr && chr.player.isKaiserNonViralSigma())
                {
                    goCollider = go.getAllColliders().FirstOrDefault(c => c.name == "head");
                    if (goCollider == null) continue;
                }

                var collideDatas = goCollider.shape.getLineIntersectCollisions(new Line(pos1, pos2));

                CollideData closestCollideData = null;
                float minDist = float.MaxValue;
                foreach (var collideData in collideDatas)
                {
                    float? dist = collideData.hitData.hitPoint?.distanceTo(pos1);
                    if (dist == null) continue;
                    if (dist.Value < minDist)
                    {
                        minDist = dist.Value;
                        closestCollideData = collideData;
                    }
                }

                if (closestCollideData != null)
                {
                    closestCollideData.otherCollider = goCollider;
                    closestCollideData.gameObject = go;
                    hits.Add(closestCollideData);
                }
            }
            return hits;
        }

        public List<CollideData> raycastAllSorted(Point pos1, Point pos2, List<Type> classNames)
        {
            var results = raycastAll(pos1, pos2, classNames);
            results.Sort((cd1, cd2) =>
            {
                float d1 = pos1.distanceTo(cd1.getHitPointSafe());
                float d2 = pos1.distanceTo(cd2.getHitPointSafe());
                if (d1 < d2) return -1;
                else if (d1 > d2) return 1;
                else return 0;
            });
            return results;
        }

        public CollideData raycast(Point pos1, Point pos2, List<Type> classNames)
        {
            var hits = raycastAll(pos1, pos2, classNames);

            float minDist = float.MaxValue;
            CollideData best = null;
            foreach (var collideData in hits)
            {
                float? dist = collideData.hitData.hitPoint?.distanceTo(pos1);
                if (dist == null) continue;
                if (dist.Value < minDist)
                {
                    minDist = dist.Value;
                    best = collideData;
                }
            }

            return best;
        }

        public List<Actor> getTargets(Point pos, int alliance, bool checkWalls, float? aMaxDist = null, bool isRequesterAI = false, bool includeAllies = false, Actor callingActor = null)
        {
            float maxDist = aMaxDist ?? Global.screenW * 0.75f;
            var targets = new List<Actor>();
            Shape shape = Rect.createFromWH(pos.x - Global.halfScreenW, pos.y - (Global.screenH * 0.75f), Global.screenW, Global.screenH).getShape();
            //DrawWrappers.DrawRectWH(pos.x - Global.halfScreenW, pos.y - (Global.screenH * 0.75f), Global.screenW, Global.screenH, true, new Color(0, 0, 255, 128), 1, ZIndex.HUD);
            var hits = Global.level.checkCollisionsShape(shape, null);
            foreach (var hit in hits)
            {
                var damagable = hit.gameObject as IDamagable;
                Actor actor = damagable?.actor();
                if (actor == null) continue;
                if (actor.pos.distanceTo(pos) > maxDist) continue;
                if (checkWalls && !noWallsInBetween(pos, actor.getCenterPos())) continue;
                if (actor == callingActor) continue;

                if (hit.gameObject is Character character)
                {
                    if (character.player.isDead) continue;
                    if (!includeAllies && character.player.alliance == alliance) continue;
                    if (character.player.alliance != alliance && character.player.isDisguisedAxl && gameMode.isTeamMode)
                    {
                        if (!isRequesterAI) continue;
                        else if (!character.disguiseCoverBlown) continue;
                    }
                    if (character.isInvisibleBS.getValue()) continue;
                }

                if (!includeAllies && !damagable.canBeDamaged(alliance, null, null)) continue;

                targets.Add(actor);
            }

            targets = targets.OrderBy(actor =>
            {
                return actor.pos.distanceTo(pos);
            }).ToList();

            return targets;
        }

        public Actor getClosestTarget(Point pos, int alliance, bool checkWalls, float? aMaxDist = null, bool isRequesterAI = false, bool includeAllies = false, Actor callingActor = null)
        {
            List<Type> filters = new List<Type>() { typeof(Character), typeof(Maverick), typeof(RaySplasher), typeof(Mechaniloid) };
            var targets = getTargets(pos, alliance, checkWalls, aMaxDist, isRequesterAI, includeAllies, callingActor);
            for (int i = 0; i < targets.Count; i++)
            {
                if (isOfClass(targets[i], filters))
                {
                    return targets[i];
                }
            }
            return null;
        }

        public bool noWallsInBetween(Point pos1, Point pos2)
        {
            var hits = raycastAll(pos1, pos2, new List<Type>() { typeof(Wall) });
            if (hits.Count == 0)
            {
                return true;
            }
            return false;
        }
    }
}
