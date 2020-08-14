using System;
using System.Linq;
using Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk {
	public sealed class MyStrategy : IStrategy {
		private const double STRIKE_ANGLE = 1.0D * Math.PI / 180.0D;
		Hockeyist self;
		private World world;
		private Game game;
		private Move move;
		private Player opponentPlayer;

		private double FieldWidth{
			get { return game.RinkRight - game.RinkLeft; }
		}

		private double NetBottom{
			get { return game.GoalNetTop + game.GoalNetHeight; }
		}

		private const double StrikeZoneShift = 110D;

		private bool ThisHasPuck {
			get { return world.Puck.OwnerHockeyistId == self.Id; }
		}

		private bool PuckUnderOurControl {
			get { return world.Puck.OwnerPlayerId == self.PlayerId; }
		}

		public void Move(Hockeyist self, World world, Game game, Move move) {
			if (self.State == HockeyistState.Swinging)
			{
				move.Action = ActionType.Strike;
				return;
			}

			Init(self, world, game, move);
			if (PuckUnderOurControl)
				if (ThisHasPuck)
				{
					if (InStrikeZone())
					{
						if (Hypot(self.SpeedX, self.SpeedY) > 0.069)
							move.SpeedUp = -1;
						if (Math.Abs(TurnToNetFarCorner(self, move)) < STRIKE_ANGLE)
							move.Action = ActionType.Swing;
					}
					else
						GoToNearestStrikeZone();
				}
				else
				{
					var nearestOpponent = GetNearestOpponent();
					if (nearestOpponent != null)
					{
						if (self.GetDistanceTo(nearestOpponent) > game.StickLength)
						{
							move.SpeedUp = 1.0D;
						}
						else if (Math.Abs(self.GetAngleTo(nearestOpponent)) < 0.5D*game.StickSector)
						{
							move.Action = ActionType.Strike;
						}
						move.Turn = self.GetAngleTo(nearestOpponent);
					}
				}
			else
			{
				move.SpeedUp = 1.0D;
				move.Turn = self.GetAngleTo(world.Puck);
				move.Action = ActionType.TakePuck;
			}
		}

		private void GoToNearestStrikeZone()
		{
			double x, y;
			GetNearestStrikeZoneCenter(out x, out y);
			var angle = self.GetAngleTo(x, y);
			if (Math.Abs(angle) > STRIKE_ANGLE)
				move.Turn = angle;
			move.SpeedUp = 1.0D;
		}

		private void GetNearestStrikeZoneCenter(out double x, out double y)
		{
			x = OpponentNetIsLeft ? game.RinkLeft + FieldWidth / 6 + StrikeZoneShift : game.RinkLeft + 5 * FieldWidth / 6 - StrikeZoneShift;
			var fieldHeight = game.RinkBottom - game.RinkTop;
			y = self.Y - (game.RinkTop + fieldHeight / 2) < 0 
				? game.RinkTop + (game.GoalNetTop - game.RinkTop) / 2
				: NetBottom + (game.RinkBottom - NetBottom) / 2;
		}

		private bool InStrikeZone()
		{
			if (OpponentNetIsLeft)
				return self.X < game.RinkLeft + FieldWidth / 3 + StrikeZoneShift 
					&& self.X > game.RinkLeft + StrikeZoneShift
					&& (self.Y < game.GoalNetTop || self.Y > NetBottom);

			return self.X > game.RinkLeft + 2 * FieldWidth / 3 - StrikeZoneShift 
				&& self.X < game.RinkRight - StrikeZoneShift
				&& (self.Y < game.GoalNetTop || self.Y > NetBottom);
		}

		private bool OpponentNetIsLeft
		{
			get { return Math.Abs(opponentPlayer.NetLeft) < 0.01D; }
		}

		private double TurnToNetFarCorner(Hockeyist self, Move move)
		{
			double x;
			double y;
			GetNetFarCorner(out x, out y);

			var angleToNet = self.GetAngleTo(x, y);
			move.Turn = angleToNet;
			return angleToNet;
		}

		private void Init(Hockeyist self, World world, Game game, Move move)
		{
			this.self = self;
			this.world = world;
			this.game = game;
			this.move = move;

			opponentPlayer = world.GetOpponentPlayer();
		}

		private void GetNetFarCorner(out double x, out double y)
		{
			x = 0.5D * (opponentPlayer.NetBack + opponentPlayer.NetFront);
			y = 0.5D * (opponentPlayer.NetBottom + opponentPlayer.NetTop);
			y += (self.Y < y ? 0.5D : -0.5D) * game.GoalNetHeight;
		}

		private Hockeyist GetNearestOpponent() {
			Hockeyist nearestOpponent = null;
			var nearestOpponentRange = 0.0D;

			foreach (var hockeyist in world.Hockeyists.Where(h => !(h.IsTeammate || h.Type == HockeyistType.Goalie || h.State == HockeyistState.KnockedDown || h.State == HockeyistState.Resting))) {
				var opponentRange =  Hypot(self.X - hockeyist.X, self.Y - hockeyist.Y);

				if (nearestOpponent == null || opponentRange < nearestOpponentRange) {
					nearestOpponent = hockeyist;
					nearestOpponentRange = opponentRange;
				}
			}

			return nearestOpponent;
		}

		private double Hypot(double side1, double side2)
		{
			return Math.Sqrt(side1 * side1 + side2 * side2);
		}
	}
}