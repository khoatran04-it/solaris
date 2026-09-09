import axiosClient from './axiosClient';
import {
  DeliveryVehicle,
  DeliveryVehiclePayload,
  DeliveryTrip,
  DeliveryTripPayload,
  TransportationDashboardStats,
  DispatchInternalPayload,
} from '../types/vehicle';

export const vehicleApi = {
  // === 1. QUẢN LÝ ĐỘI XE (VEHICLES) ===
  getAllVehicles: (params?: {
    vehicleType?: string;
    status?: string;
  }): Promise<DeliveryVehicle[]> => {
    return axiosClient.get('/vehicles', { params });
  },

  getVehicleById: (id: number): Promise<DeliveryVehicle> => {
    return axiosClient.get(`/vehicles/${id}`);
  },

  createVehicle: (data: DeliveryVehiclePayload): Promise<DeliveryVehicle> => {
    return axiosClient.post('/vehicles', data);
  },

  updateVehicle: (id: number, data: DeliveryVehiclePayload): Promise<DeliveryVehicle> => {
    return axiosClient.put(`/vehicles/${id}`, data);
  },

  deleteVehicle: (id: number): Promise<void> => {
    return axiosClient.delete(`/vehicles/${id}`);
  },

  // === 2. QUẢN LÝ CHUYẾN XE & ĐIỀU PHỐI (DELIVERY TRIPS) ===
  getAllTrips: (params?: { status?: string; tripType?: string }): Promise<DeliveryTrip[]> => {
    return axiosClient.get('/delivery-trips', { params });
  },

  getTripById: (id: number): Promise<DeliveryTrip> => {
    return axiosClient.get(`/delivery-trips/${id}`);
  },

  getDashboardStats: (): Promise<TransportationDashboardStats> => {
    return axiosClient.get('/delivery-trips/dashboard-stats');
  },

  createTrip: (data: DeliveryTripPayload): Promise<DeliveryTrip> => {
    return axiosClient.post('/delivery-trips', data);
  },

  startTrip: (id: number): Promise<DeliveryTrip> => {
    return axiosClient.post(`/delivery-trips/${id}/start`);
  },

  markOrderDelivered: (tripId: number, orderId: number, note?: string): Promise<DeliveryTrip> => {
    return axiosClient.post(`/delivery-trips/${tripId}/orders/${orderId}/deliver`, { note });
  },

  markOrderFailed: (tripId: number, orderId: number, reason: string): Promise<DeliveryTrip> => {
    return axiosClient.post(`/delivery-trips/${tripId}/orders/${orderId}/failed`, { reason });
  },

  completeTrip: (id: number): Promise<DeliveryTrip> => {
    return axiosClient.post(`/delivery-trips/${id}/complete`);
  },

  // === 3. ĐIỀU PHỐI ĐƠN HÀNG LẺ (GÁN XE NỘI BỘ TỪ CHI TIẾT ĐƠN HÀNG) ===
  dispatchOrderInternal: (
    orderId: number,
    data: DispatchInternalPayload
  ): Promise<DeliveryTrip> => {
    return axiosClient.post(`/orders/${orderId}/dispatch-internal`, data);
  },
};
