import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, LeaveRequest, LeaveType } from '../../services/auth.service';
@Component({selector:'app-my-leaves',standalone:true,imports:[CommonModule,FormsModule,RouterLink],templateUrl:'./my-leaves.html',styleUrl:'./my-leaves.css'})
export class MyLeaves implements OnInit {
 private api=inject(AuthService); private router=inject(Router); private cdr=inject(ChangeDetectorRef);
 username=this.api.getUsername(); role=this.api.getRole(); requests:LeaveRequest[]=[]; types:LeaveType[]=[]; loading=false; error=''; search=''; statusFilter='All'; cancelNotice='';
 ngOnInit(){this.api.getLeaveTypes().subscribe({next:x=>{this.types=x;this.cdr.markForCheck();},error:()=>{}});this.load();}
 load(){const id=this.api.getEmployeeId();if(!id){this.error='Employee ID missing from access token. Please sign in again.';return;}this.loading=true;this.error='';this.api.getMyLeaveRequests(id).subscribe({next:x=>{this.requests=x;this.loading=false;this.cdr.markForCheck();},error:e=>{this.error=e.error?.message||`Unable to load requests (${e.status}).`;this.loading=false;this.cdr.markForCheck();}});}
 get filtered(){const q=this.search.toLowerCase();return this.requests.filter(r=>(this.statusFilter==='All'||r.status.toLowerCase()===this.statusFilter.toLowerCase())&&(`${r.reason} ${this.leaveType(r.leaveTypeId)} ${r.leaveRequestId}`).toLowerCase().includes(q)).sort((a,b)=>new Date(b.createdAtUtc).getTime()-new Date(a.createdAtUtc).getTime());}
 leaveType(id:number){return this.types.find(t=>t.leaveTypeId===id)?.name||`Leave Type ${id}`;}
 canCancel(item:LeaveRequest){const today=new Date().toISOString().split('T')[0];if(item.status==='Pending')return true;return item.status==='Approved'&&item.startDate>today;}
 cancelRequest(item:LeaveRequest){if(!window.confirm(`Cancel leave request #${item.leaveRequestId} (${item.startDate} – ${item.endDate})? This cannot be undone.`))return;this.error='';this.cancelNotice='';this.api.cancelLeaveRequest(item.leaveRequestId).subscribe({next:()=>{this.cancelNotice=`Leave request #${item.leaveRequestId} was cancelled.`;this.load();},error:e=>{this.error=e.error?.message||`Unable to cancel request (${e.status}).`;this.cdr.markForCheck();}});}
 logout(){this.api.logout();this.router.navigate(['/login']);}
}