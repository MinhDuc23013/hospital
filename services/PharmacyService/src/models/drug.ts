import { DataTypes, Model, Optional } from 'sequelize';
import { sequelize } from '../database/sequelize';

interface DrugAttributes {
  id: number;
  code: string;
  name: string;
  dosage?: string;
  unit?: string;
  price?: number;
  currentStock: number;
  minimumStock: number;
  supplier?: string;
  expirationDate?: Date;
}

interface DrugCreationAttributes extends Optional<DrugAttributes, 'id' | 'currentStock' | 'minimumStock'> {}

export class Drug extends Model<DrugAttributes, DrugCreationAttributes> implements DrugAttributes {
  declare id: number;
  declare code: string;
  declare name: string;
  declare dosage?: string;
  declare unit?: string;
  declare price?: number;
  declare currentStock: number;
  declare minimumStock: number;
  declare supplier?: string;
  declare expirationDate?: Date;
}

Drug.init({
  id: { type: DataTypes.INTEGER, primaryKey: true, autoIncrement: true },
  code: { type: DataTypes.STRING(50), unique: true, allowNull: false },
  name: { type: DataTypes.STRING(200), allowNull: false },
  dosage: DataTypes.STRING(100),
  unit: DataTypes.STRING(50),
  price: DataTypes.DECIMAL(10, 2),
  currentStock: { type: DataTypes.INTEGER, defaultValue: 0 },
  minimumStock: { type: DataTypes.INTEGER, defaultValue: 10 },
  supplier: DataTypes.STRING(200),
  expirationDate: DataTypes.DATE,
}, {
  sequelize,
  tableName: 'drugs',
  timestamps: true,
  underscored: true,
});
