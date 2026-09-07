export interface DeliveryVehicle {
  id: number;
  code: string;
  licensePlate: string;
  vehicleType: 'Motorbike' | 'RefrigeratedTruck' | string;
  maxWeightKg: number;
  isColdChainEquipped: boolean;
  status: 'Available' | 'OnTrip' | 'Maintenance' | string;
  driverName?: string;
  driverPhone?: string;
  homeWarehouseId?: number;
  homeWarehouseName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface DeliveryVehiclePayload {
  code?: string;
  licensePlate: string;
  vehicleType: string;
  maxWeightKg: number;
  isColdChainEquipped: boolean;
  status: string;
  driverName?: string;
  driverPhone?: string;
  homeWarehouseId?: number;
  isActive: boolean;
}

export interface DeliveryTripOrder {
  id: number;
  orderId: number;
  orderCode: string;
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  totalAmount: number;
  deliverySequence: number;
  status: 'Pending' | 'Delivered' | 'Failed' | string;
  deliveredAt?: string;
  failureReason?: string;
  note?: string;
}

export interface DeliveryTrip {
  id: number;
  tripCode: string;
  tripType: 'B2C_Delivery' | 'B2B_Transfer' | string;
  status: 'Preparing' | 'InTransit' | 'Completed' | 'Cancelled' | string;
  vehicleId: number;
  vehicleCode: string;
  vehicleType: string;
  licensePlate: string;
  driverName: string;
  driverPhone: string;
  warehouseId: number;
  warehouseName: string;
  startedAt?: string;
  completedAt?: string;
  note?: string;
  totalOrders: number;
  deliveredOrders: number;
  inventoryTransferId?: number;
  transferCode?: string;
  createdAt: string;
  orders: DeliveryTripOrder[];
}

export interface DeliveryTripPayload {
  vehicleId: number;
  warehouseId: number;
  tripType: string;
  note?: string;
  orderIds: number[];
  inventoryTransferId?: number;
}

export interface OrderWaitingDispatch {
  orderId: number;
  orderCode: string;
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  totalAmount: number;
  requiresColdChain: boolean;
  createdAt: string;
}

export interface TransportationDashboardStats {
  availableBikes: number;
  totalBikes: number;
  activeTrucks: number;
  totalTrucks: number;
  pendingColdChainOrders: number;
  activeTripsCount: number;
  recentActiveTrips: DeliveryTrip[];
  ordersWaitingDispatch: OrderWaitingDispatch[];
}

export interface DispatchInternalPayload {
  vehicleId: number;
  note?: string;
}
