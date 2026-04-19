import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  CalendarOutlined,
  FileTextOutlined,
  MedicineBoxOutlined,
  SafetyOutlined,
  LineChartOutlined,
  CreditCardOutlined,
  CheckCircleOutlined,
  HistoryOutlined,
  ArrowRightOutlined,
} from '@ant-design/icons';
import type { ComponentType } from 'react';
import { useAuth } from '../../auth/useAuth';
import { appointmentApi } from '../appointment/appointment-api';

export default function PatientDashboard() {
  const { userId, fullName, email } = useAuth();

  const { data: appts } = useQuery({
    queryKey: ['appointments', 'my', userId],
    queryFn: () => appointmentApi.list(userId, undefined, 1, 20),
    enabled: !!userId,
  });

  const list = appts?.data ?? [];
  const upcoming = list.filter(a => a.status === 'Scheduled' && new Date(a.scheduledTime) > new Date());
  const completed = list.filter(a => a.status === 'Completed');
  const next = upcoming.sort((a, b) => new Date(a.scheduledTime).getTime() - new Date(b.scheduledTime).getTime())[0];

  return (
    <div>
      {/* Hero greeting */}
      <div className="bg-gradient-to-br from-emerald-700 to-emerald-900 rounded-2xl p-6 md:p-8 text-white mb-8 shadow-lg">
        <h1 className="text-2xl md:text-3xl font-bold mb-2">
          Welcome back, {fullName?.split(' ')[0] || 'there'}
        </h1>
        <p className="text-emerald-100 text-sm mb-4">{email}</p>

        {next ? (
          <div className="mt-4 bg-white/10 backdrop-blur rounded-lg p-4 border border-white/20">
            <div className="text-xs text-emerald-200 uppercase tracking-wide mb-1 flex items-center gap-1.5">
              <CalendarOutlined /> Next Appointment
            </div>
            <div className="text-lg font-semibold">
              {new Date(next.scheduledTime).toLocaleString('vi-VN', {
                weekday: 'short', day: '2-digit', month: 'short',
                hour: '2-digit', minute: '2-digit',
              })}
            </div>
            <div className="text-sm text-emerald-100">{next.durationMinutes} minutes · {next.status}</div>
          </div>
        ) : (
          <Link
            to="/appointments"
            className="mt-4 inline-flex items-center gap-2 px-5 py-2.5 bg-white text-emerald-700 rounded-lg font-medium hover:bg-emerald-50"
          >
            Book Your First Appointment <ArrowRightOutlined />
          </Link>
        )}
      </div>

      {/* Quick stats */}
      <div className="grid grid-cols-3 gap-4 mb-8">
        <StatCard Icon={CalendarOutlined} label="Upcoming" value={upcoming.length} accent="bg-emerald-50 text-emerald-700 border-emerald-200" />
        <StatCard Icon={CheckCircleOutlined} label="Completed" value={completed.length} accent="bg-green-50 text-green-700 border-green-200" />
        <StatCard Icon={HistoryOutlined} label="Total Visits" value={list.length} accent="bg-teal-50 text-teal-700 border-teal-200" />
      </div>

      {/* Feature cards */}
      <h2 className="text-xl font-bold text-gray-900 mb-4">Your Health Hub</h2>
      <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
        <FeatureCard
          to="/appointments"
          Icon={CalendarOutlined}
          title="Appointment Booking"
          desc="Book consultations with doctors. Real-time slot availability."
          cta={upcoming.length > 0 ? `${upcoming.length} upcoming` : 'Book now'}
        />
        <FeatureCard
          to="/medical-records"
          Icon={FileTextOutlined}
          title="Medical Records"
          desc="Access your complete medical history anytime."
          cta="View records"
        />
        <FeatureCard
          to="/pharmacy"
          Icon={MedicineBoxOutlined}
          title="E-Prescriptions"
          desc="Digital prescriptions ready at partner pharmacies."
          cta="My prescriptions"
        />
        <FeatureCard
          Icon={SafetyOutlined}
          title="Privacy First"
          desc="End-to-end encryption. HIPAA-compliant protection."
          cta="Security settings"
          disabled
        />
        <FeatureCard
          Icon={LineChartOutlined}
          title="Health Analytics"
          desc="Track your vitals, medications, and health trends."
          cta="Coming soon"
          disabled
        />
        <FeatureCard
          to="/billing"
          Icon={CreditCardOutlined}
          title="Billing & Payments"
          desc="View invoices, payment history, and insurance claims."
          cta="View bills"
        />
      </div>
    </div>
  );
}

function StatCard({ Icon, label, value, accent }: {
  Icon: ComponentType<{ className?: string }>;
  label: string;
  value: number | string;
  accent: string;
}) {
  return (
    <div className={`rounded-xl p-5 border ${accent}`}>
      <Icon className="text-2xl mb-2" />
      <div className="text-2xl font-bold">{value}</div>
      <div className="text-xs font-medium opacity-80 mt-0.5">{label}</div>
    </div>
  );
}

function FeatureCard({ to, Icon, title, desc, cta, disabled }: {
  to?: string;
  Icon: ComponentType<{ className?: string }>;
  title: string;
  desc: string;
  cta: string;
  disabled?: boolean;
}) {
  const body = (
    <div
      className={`bg-white rounded-xl border border-gray-200 p-5 transition-all h-full ${
        disabled
          ? 'opacity-60'
          : 'hover:border-emerald-400 hover:shadow-md hover:shadow-emerald-100 cursor-pointer'
      }`}
    >
      <Icon className="text-3xl text-emerald-700 mb-3" />
      <h3 className="font-semibold text-gray-900 mb-1">{title}</h3>
      <p className="text-sm text-gray-600 mb-4 leading-relaxed">{desc}</p>
      <div className={`text-sm font-medium flex items-center gap-1 ${disabled ? 'text-gray-400' : 'text-emerald-700'}`}>
        {cta} {!disabled && <ArrowRightOutlined />}
      </div>
    </div>
  );

  if (disabled || !to) return <div>{body}</div>;
  return <Link to={to}>{body}</Link>;
}
